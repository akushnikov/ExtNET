using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;


namespace ExtNET.Web.Helpers.Bundling
{
    public class DependencyNameComparer : IEqualityComparer<string>
    {
        public Boolean Equals(string x, string y)
        {
            return StringComparer.Ordinal.Equals(Normalize(x), Normalize(y));
        }

        public int GetHashCode(string value)
        {
            return StringComparer.Ordinal.GetHashCode(Normalize(value));
        }

        private string Normalize(string value)
        {
            value = value.ToLowerInvariant();
            value = value.Replace("-", ".");
            value = value.Replace(".d.ts", ".js");
            value = value.Replace(".ts", ".js");
            value = value.Replace(".min.", ".");
            value = value.Replace(".pack.", ".");
            value = value.Replace(".custom.", ".");
            value = value.Replace(".intellisense.", ".");
            value = value.Replace(".vsdoc.", ".");
            value = Regex.Replace(value, @"\.(([0-9]*|[A-Za-z])\.)+", ".");
            return value;
        }
    }
}