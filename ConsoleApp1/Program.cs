using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CommandLine;

namespace ConsoleApp1
{
    public class AppOptions
    {
        [Option("bla", Default = true)]
        public bool Bla { get; set; } = true;

        [Option("blub", Default = "hallo Sie")]
        public string Blub { get; set; }

        [Option("num")]
        public int Num { get; set; } = 3; // probably best to set Default, too

        [Option("test")]
        public List<TestDisp> Test { get; set; }

        [Option("test2")]
        public TestDisp test2 { get; set; }
    }

    public class TestDisp : IDisposable
    {
        public string str;

        public TestDisp(string str)
        {
            this.str = str;
        }

        public void Dispose()
        {

        }

        public override string ToString()
        {
            return str;
        }
    }

    class Program
    {
        public static void Main(string[] args)
        {
            using (Parser parser = new Parser(
                with =>
                {
                    with.IgnoreUnknownArguments = true;
                    with.CaseSensitive = false;
                }))
            {
                ParserResult<AppOptions> r = parser.ParseArguments<AppOptions>(args);

                if (r is Parsed<AppOptions> parsed)
                {
                    var dis = parsed.DisposableOptions;
                }

                r.WithParsed(op =>
                {
                    var unparser = new UnParser();
                    string str = unparser.FormatCommandLine(op);
                });
            }
        }

        [Verb("add", HelpText = "Add file contents to the index.")]
        public class AddOptions
        {
            //normal options here
            [Option("bla")]
            public bool Bla { get; set; }

            [Option("blub")]
            public string Blub { get; set; }
        }
        [Verb("commit", HelpText = "Record changes to the repository.")]
        public class CommitOptions
        {
            //commit options here
        }
        [Verb("clone", HelpText = "Clone a repository into a new directory.")]
        public class CloneOptions
        {
            //clone options here
        }

        public static int MainTestVerbs(string[] args)
        {
            using (Parser parser = new Parser(
                with =>
                {
                    with.IgnoreUnknownArguments = true;
                    with.CaseSensitive = false;
                }))
            {
                return parser.ParseArguments<AddOptions, CommitOptions, CloneOptions>(args)
                    .MapResult(
                        (AddOptions opts) => RunAddAndReturnExitCode(opts),
                        (CommitOptions opts) => RunCommitAndReturnExitCode(opts),
                        (CloneOptions opts) => RunCloneAndReturnExitCode(opts),
                        errs => handleErrors(errs));
            }
        }

        private static int handleErrors(IEnumerable<Error> opts)
        {
            return 1;
        }

        private static int RunCloneAndReturnExitCode(CloneOptions opts)
        {
            return 0;
        }

        private static int RunCommitAndReturnExitCode(CommitOptions opts)
        {
            return 0;
        }

        private static int RunAddAndReturnExitCode(AddOptions opts)
        {
            return 0;
        }
    }



}
