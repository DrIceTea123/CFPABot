﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GammaLibrary.Enhancements;
using GammaLibrary.Extensions;
using LibGit2Sharp;
using Serilog;

namespace CFPABot.Utils
{
    public record RepoFileAnalyzeResult(string Branch, string FilePath, string FileName, LangType Type, string CommitSha);

    public record RepoAnalyzeResult(string RepoGitHubLink, RepoFileAnalyzeResult[] Results, string Owner, string RepoName);
    
    public enum LangType
    {
        CN, EN
    }

    public static class RepoAnalyzer
    {

        static Dictionary<string, CommentBuilderLock> locks = new();
        static async ValueTask<CommentBuilderLock> AcquireLock(string lockName)
        {
            CommentBuilderLock l;
            lock (typeof(RepoAnalyzer))
            {
                if (!locks.ContainsKey(lockName)) locks[lockName] = new CommentBuilderLock();
                l = locks[lockName];
            }
            await l.WaitAsync();
            return l;
        }

        public static async Task<RepoAnalyzeResult> Analyze(string githubLink)
        {
            using var l = await AcquireLock(githubLink);

            var repoPath = "caches/" + Guid.NewGuid().ToString("N");
            var sp = githubLink.Split('/');
            var owner = sp[^2];
            var repoName = sp[^1];
            var resultPath = $"config/repo_analyze_results/{owner}.{repoName}.json";
            if (File.Exists(resultPath)) return JsonSerializer.Deserialize<RepoAnalyzeResult>(resultPath);
            
            var githubBranches = await GitHub.Instance.Repository.Branch.GetAll(owner, repoName);

            Repository.Clone(githubLink + ".git", repoPath);
            var repo = new Repository(repoPath);
            var results = new List<RepoFileAnalyzeResult>();

            foreach (var branchName in githubBranches.Select(b => b.Name))
            {
                SwitchBranch(repo, branchName);
                foreach (var filePath in Directory.EnumerateFiles(repoPath, "*.*", SearchOption.AllDirectories))
                {
                    var fileName = Path.GetFileName(filePath);
                    var relativePath = Path.GetRelativePath(repoPath, filePath);
                    if (fileName.Equals("en_us.lang", StringComparison.OrdinalIgnoreCase) || fileName.Equals("en_us.json", StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(new RepoFileAnalyzeResult(branchName, relativePath, fileName, LangType.EN, repo.Head.Tip.Sha));
                    }
                    if (fileName.Equals("zh_cn.lang", StringComparison.OrdinalIgnoreCase) || fileName.Equals("zh_cn.json", StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(new RepoFileAnalyzeResult(branchName, relativePath, fileName, LangType.CN, repo.Head.Tip.Sha));
                    }
                }
            }
            repo.Dispose();
            try
            {
                Directory.Delete(repoPath, true);
            }
            catch (Exception e)
            {
                Log.Error(e, $"删除 repo 失败: {githubLink}");
            }

            var result = new RepoAnalyzeResult(githubLink, results.ToArray(), owner, repoName);
            File.WriteAllText(resultPath, result.ToJsonString());
            return result;
        }


        // https://stackoverflow.com/questions/46588604/checking-out-remote-branch-using-libgit2sharp
        static Branch SwitchBranch(Repository repo, string branchName)
        {
            var branch = repo.Branches[branchName];

            if (branch == null)
            {
                // Repository returns null object when branch does not exist
                // so, create a new, LOCAL branch
                repo.CreateBranch(branchName); // Or repo.Branches.Add("dev", "HEAD");
                branch = repo.Branches[branchName];

                // You will need more code _here_ if you want to synchronize/set 
                // an upstream branch...
            }

            // Now, checkout the branch
            return Commands.Checkout(repo, branch);
        }
    }
}
