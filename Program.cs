using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace QueryAnalyzer
{
    class Query
    {
        public string Statement { get; set; }
        public int RuntimeInMS { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length < 1)
            {
                Console.Error.WriteLine("FilePath parameter is required");
                return;
            }

            var filePath = args[0];
            var lines = File.ReadAllLines(filePath);
            CountQueries(lines);
            Console.WriteLine("------------------------");
            Console.WriteLine("Distinct SELECT clauses occuring more than 3 times");
            FindDistinctSelectSimple(lines);

            Console.WriteLine("------------------------");
            Console.WriteLine("Distinct queries occurring more than 3 times");
            FindDistinctQueries(lines);

            Console.WriteLine("------------------------");
            Console.WriteLine("Distinct FROM clauses occurring more than 3 times");
            FindDistinctFroms(lines);

            Console.WriteLine("------------------------");
            Console.WriteLine("Queries taking longer than the average query runtime");
            FindLongRunningQueries(lines);

            Console.WriteLine("------------------------");
        }

        static void CountQueries(string[] lines)
        {
            var numQueries = lines.Count(x => x.Trim().StartsWith("Executed DbCommand"));
            Console.WriteLine($"{numQueries} total queries");
        }

        static void FindDistinctSelectSimple(string[] lines)
        {
            var selects = lines.Where(line => line.Trim().StartsWith("SELECT"));///.Select(x => x.Substring(0, Math.Min(x.Length, 100)));
            var selectCounts = selects
                .GroupBy(s => s)
                .OrderByDescending(g => g.Count())
                .Where(g => g.Count() > 3)
                .Select(g => new { Count = g.Count(), Select = g.Key });
            foreach (var selectCount in selectCounts)
            {
                Console.WriteLine($"{selectCount.Count}\t{selectCount.Select}");
            }
        }

        static void FindDistinctQueries(string[] lines)
        {
            var queries = GetQueries(lines);

            var queryCounts = queries
                .GroupBy(s => s.Statement)
                .OrderByDescending(g => g.Count())
                .Where(g => g.Count() > 3)
                .Select(g => new { Count = g.Count(), Query = g.Key });
            foreach (var queryCount in queryCounts)
            {
                Console.WriteLine($"{queryCount.Count}\t{queryCount.Query}");
            }
        }

        static void FindDistinctFroms(string[] lines)
        {
            var froms = new List<string>();
            bool foundFrom = false;
            foreach (var line in lines)
            {
                var lineTrimmed = line.Trim();
                if (lineTrimmed.StartsWith("info:"))
                {
                    foundFrom = false;
                    continue;
                }

                if (!foundFrom && lineTrimmed.StartsWith("FROM"))
                {
                    foundFrom = true;
                    froms.Add(lineTrimmed);
                }
            }

            var queryCounts = froms
                .GroupBy(s => s)
                .OrderByDescending(g => g.Count())
                .Where(g => g.Count() > 3)
                .Select(g => new { Count = g.Count(), Query = g.Key });
            foreach (var queryCount in queryCounts)
            {
                Console.WriteLine($"{queryCount.Count}\t{queryCount.Query}");
            }
        }

        static void FindLongRunningQueries(string[] lines)
        {
            var queries = GetQueries(lines);

            var avgRuntime = queries.Average(x => x.RuntimeInMS);
            Console.WriteLine($"Average query runtime: {avgRuntime}");

            var longRunningQueries = queries
                .Where(q => q.RuntimeInMS > avgRuntime);
            var queryAggs = longRunningQueries
                .GroupBy(q => q.Statement)
                .OrderByDescending(g => g.First().RuntimeInMS)
                .Select(g => new { Count = g.Count(), AvgRuntime = g.Average(x => x.RuntimeInMS), Query = g.Key });
            foreach (var queryAgg in queryAggs)
            {
                Console.WriteLine($"{queryAgg.Count}\t{queryAgg.AvgRuntime}ms\t{queryAgg.Query}");
            }
        }

        static List<Query> GetQueries(string[] lines)
        {
            var queries = new List<Query>();
            Query currentQuery = null;
            bool readingQuery = false;
            foreach (var line in lines)
            {
                var lineTrimmed = line.Trim();
                if (lineTrimmed.StartsWith("info:"))
                {
                    if (currentQuery != null)
                    {
                        queries.Add(currentQuery);
                        currentQuery = null;
                        readingQuery = false;
                    }
                    continue;
                }

                if (!readingQuery && lineTrimmed.StartsWith("Executed DbCommand"))
                {
                    readingQuery = true;
                    var match = Regex.Match(lineTrimmed, @"Executed DbCommand \(([\d,]+)ms\)");
                    currentQuery = new Query()
                    {
                        RuntimeInMS = int.Parse(match.Groups[1].Value.Replace(",", ""))
                    };
                    continue;
                }



                if (readingQuery)
                {
                    currentQuery.Statement = string.Join(Environment.NewLine, currentQuery.Statement, lineTrimmed);
                }
            }
            return queries;
        }
    }
}
