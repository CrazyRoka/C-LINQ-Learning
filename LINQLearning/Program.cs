using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LINQLearning
{
    class Program
    {
        static void Main(string[] args)
        {
            Expression<Func<Racer, bool>> expression = r => r.Country == "Brazil" && r.Wins > 6;
            ExpressionTree.DisplayTree(0, "Lambda", expression);
        }

        static void GenerateRange()
        {
            var values = Enumerable.Range(1, 20);
            foreach (var item in values)
            {
                Console.WriteLine(item);
            }
        }

        static IList<int> SampleData()
        {
            const int arraySize = 50_000_000;
            var random = new Random();
            return Enumerable.Range(0, arraySize).Select(x => random.Next(150)).ToList();
        }

        static void ParralelLinqQuery(IEnumerable<int> data)
        {
            var res = (from x in data.AsParallel()
                       where Math.Log(x) < 4
                       select x).Average();
            Console.WriteLine(res);
        }

        static void UseCancellation(IEnumerable<int> data)
        {
            CancellationTokenSource cts = new CancellationTokenSource();

            Task.Run(() =>
            {
                try
                {
                    var res = (from x in data.AsParallel().WithCancellation(cts.Token)
                               where Math.Log(x) < 4
                               select x).Average();
                    Console.WriteLine($"Query finished with result {res}");
                }
                catch(OperationCanceledException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            });
            Console.WriteLine("Query started");
            Console.WriteLine("Cancel? ");
            string input = Console.ReadLine();
            if (input.ToLower().Equals("y"))
            {
                cts.Cancel();
            }
            Console.ReadLine();
        }

        static void UseAPartitioner(IList<int> data)
        {
            var result = (from x in Partitioner.Create(data, true).AsParallel()
                          where Math.Log(x) < 4
                          select x).Average();

            Console.WriteLine(result);
        }

        static void CompoundFrom()
        {
            var drivers = from r in Formula1.GetChampions()
                          from c in r.Cars
                          where c != "BRM"
                          orderby r.LastName
                          select r.FirstName + " " + r.LastName;

            foreach(string s in drivers)
            {
                Console.WriteLine(s);
            }
        }

        static void Partitioning()
        {
            int pageSize = 5;
            int numberPages = (int)Math.Ceiling(Formula1.GetChampions().Count / (double)pageSize);

            for(int page = 0; page < numberPages; page++)
            {
                Console.WriteLine($"Page {page}");

                var racers = (from r in Formula1.GetChampions()
                              orderby r.LastName, r.FirstName
                              select r.FirstName + " " + r.LastName)
                              .Skip(page * pageSize).Take(pageSize);
                
                foreach(var name in racers)
                {
                    Console.WriteLine(name);
                }
                Console.WriteLine();
            }
        }

        static void Filtering()
        {
            var racers = from r in Formula1.GetChampions()
                         where r.Wins > 15 &&
                         (r.Country == "UK" || r.Country == "USA")
                         select r;

            foreach (Racer r in racers)
            {
                Console.WriteLine($"{r:A}");
            }
        }

        static void FilteringWithIndex()
        {
            var racers = Formula1.GetChampions()
                .Where((r, index) => r.LastName.StartsWith("H") && index % 2 != 0);

            foreach (Racer r in racers)
            {
                Console.WriteLine($"{r:A}");
            }
        }

        static void TypeFiltering()
        {
            object[] data = { "one", 2, 3, "four", "five", 6 };
            var query = data.OfType<int>();

            foreach (int s in query)
            {
                Console.WriteLine(s);
            }
        }

        static void CompoundFromWithMethods()
        {
            var drivers = Formula1.GetChampions()
                .SelectMany(r => r.Cars, (r, c) => new { Racer = r, Car = c })
                .Where(r => r.Car != "BRM")
                .OrderBy(r => r.Racer.LastName)
                .Select(r => r.Racer.FirstName + " " + r.Racer.LastName);

            foreach(string s in drivers)
            {
                Console.WriteLine(s);
            }
        }

        static void LeftOuterJoin()
        {
            var racers = from r in Formula1.GetChampions()
                         from y in r.Years
                         select new
                         {
                             Year = y,
                             Name = r.FirstName + " " + r.LastName
                         };
            var teams = from t in Formula1.GetContructorChampions()
                        from y in t.Years
                        select new
                        {
                            Year = y,
                            Name = t.Name
                        };


            var racersAndTeams = from r in racers
                                 join t in teams on r.Year equals t.Year into rt
                                 from t in rt.DefaultIfEmpty()
                                 orderby r.Year
                                 select new
                                 {
                                     r.Year,
                                     Champion = r.Name,
                                     Constructor = t?.Name ?? "no constructor championship"
                                 };

            foreach(var rt in racersAndTeams)
            {
                Console.WriteLine($"{rt.Year} {rt.Champion} {rt.Constructor}");
            }
        }

        static void AggregateSum()
        {
            var countries = (from t in (from r in Formula1.GetChampions()
                                        group r by r.Country into c
                                        select new
                                        {
                                            Country = c.Key,
                                            Wins = (from r1 in c
                                                    select r1.Wins).Sum()
                                        })
                            orderby t.Wins descending, t.Country
                            select t).Take(5);

            foreach(var country in countries)
            {
                Console.WriteLine($"{country.Country} {country.Wins}");
            }
        }

        static void LINQQuery()
        {
            var query = from r in Formula1.GetChampions()
                        where r.Country == "UK"
                        orderby r.Wins descending
                        select r;

            foreach (Racer r in query)
            {
                Console.WriteLine($"{r:A}");
            }
        }
    }
}
