using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Hosting;
using System.Web.Optimization;

namespace ExtNET.Web.Helpers.Bundling
{
    public class ScriptDependencyOrderer : IBundleOrderer
    {
        public ScriptDependencyOrderer()
        {
            EqualityComparer = new DependencyNameComparer();
        }

        public ScriptDependencyOrderer(IEqualityComparer<string> equalityComparer)
        {
            EqualityComparer = equalityComparer;
        }

        public ScriptDependencyOrderer(IEnumerable<string> excludedDependencies)
            : this()
        {
            ExcludedDependencies = excludedDependencies;
        }

        public ScriptDependencyOrderer(IEqualityComparer<string> equalityComparer, IEnumerable<string> excludedDependencies)
            : this(equalityComparer)
        {
            ExcludedDependencies = excludedDependencies;
        }

        private IEqualityComparer<string> EqualityComparer { get; set; }

        private static readonly Regex ReferenceRegex = new Regex(@"///\s*<reference\s+path=""(?<path>[^""]*)""\s*/>");

        public IEnumerable<string> ExcludedDependencies { get; set; }

        public IEnumerable<BundleFile> OrderFiles(BundleContext context, IEnumerable<BundleFile> files)
        {
            var workingItems = files.AsParallel().Select(item => new WorkingItem
            {
                Path = item.VirtualFile.VirtualPath,
                BundleFile = item,
                Dependencies = GetDependecies(item.VirtualFile)
            });

            var fileDependencies = new Dictionary<string, WorkingItem>(EqualityComparer);
            foreach (var item in workingItems)
            {
                WorkingItem duplicate;
                if (fileDependencies.TryGetValue(item.Path, out duplicate))
                    throw new ArgumentException(String.Format("During dependency resolution, a collision between '{0}' and '{1}' was detected. Files in a bundle must not collide with respect to the dependency name comparer.", Path.GetFileName(item.Path), Path.GetFileName(duplicate.Path)));

                fileDependencies.Add(item.Path, item);
            }

            foreach (var item in fileDependencies.Values)
            {
                foreach (var dependency in item.Dependencies.Where(dependency => !fileDependencies.ContainsKey(dependency)))
                {
                    throw new ArgumentException(String.Format("Dependency '{0}' referenced by '{1}' could not found. Ensure the dependency is part of the bundle and its name can be detected by the dependency name comparer. If the dependency is not supposed to be in the bundle, add it to the list of excluded dependencies.", Path.GetFileName(dependency), Path.GetFileName(item.Path)));
                }
            }

            while (fileDependencies.Count > 0)
            {
                var result = fileDependencies.Values.FirstOrDefault(f => f.Dependencies.All(d => !fileDependencies.ContainsKey(d)));
                if (result == null)
                    throw new ArgumentException(String.Format("During dependency resolution, a cyclic dependency was detected among the remaining dependencies {0}.", String.Join(", ", fileDependencies.Select(d => "'" + Path.GetFileName(d.Value.Path) + "'"))));
                yield return result.BundleFile;
                fileDependencies.Remove(result.Path);
            }
        }

        private IEnumerable<string> GetDependecies(VirtualFile virtualFile)
        {
            var directory = VirtualPathUtility.GetDirectory(virtualFile.VirtualPath);
            string content;

            using (var stream = virtualFile.Open())
            using (var reader = new StreamReader(stream))
            {
                content = reader.ReadToEnd();
            }

            return ReferenceRegex.Matches(content).Cast<Match>().Select(m =>
            {
                var relativePath = m.Groups["path"].Value;
                return VirtualPathUtility.Combine(directory, relativePath);
            }).Where(m => ExcludedDependencies.All(e => !m.Contains(@"/" + e))).ToArray();
        }

        private sealed class WorkingItem
        {
            public string Path { get; set; }
            public BundleFile BundleFile { get; set; }
            public IEnumerable<string> Dependencies { get; set; }
        }
    }
}