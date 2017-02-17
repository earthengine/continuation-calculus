using Continuation_Calculus.Utils;
using Continuation_Calculus.Version1;
using NUnit.Framework;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Continuation_Calculus_Test
{
    [TestFixture]
    public class UnitTest1
    {
        [Test]
        public void TestYExp()
        {
//                        var p = Program1.FromExpression<Func<Func<Func<int,int>, Func<int,int>>, Func<int,int>>>(
//                            f => ((Loop<Func<int,int>>)(w => w(w)))(w => f((i => w(w)(i)))));
            var p = Program.FromExpression(ExpressionHelper.YExp<Func<int, int>>());
            var t = p.ToString();

            ITerm term = new Term(Atom.Entry("r33"), new[] { Atom.Parameter("f"), Atom.Parameter("r") });
            ProgramExecution.EvalRule(p, term);
            var sb = new StringBuilder();
            term = p.Run(term, sb);

            term = term.Args.First();
            term = new Term(Atom.Entry(term.Head), term.Args.Concat(new[] { Atom.Parameter("i"), Atom.Parameter("r") }));
            sb.AppendLine();
            term = p.Run(term, sb);

            term = term.Args.Last();
            term = new Term(Atom.Entry(term.Head), term.Args.Concat(new[] { Atom.Parameter("v") }));
            sb.AppendLine();
            term = p.Run(term, sb);
        }
        
        [Test]
        public void TestRecursion()
        {
            var e = ExpressionHelper.Apply(ExpressionHelper.YExp<Func<int, int>>(), f => i => i > 0 ? 1 : f(i-1) * i);
            var p = Program.FromExpression(e);
            var t = p.ToString();

            ITerm term = new Term(Atom.Entry("r69"), new[] { Atom.Parameter("v0"), Atom.Parameter("r") });
            var sb = new StringBuilder();
            term = p.Run(term, sb);

            term = term.Args.Last();
            term = new Term(term.GetHead(), term.Args.Concat(new[] { Atom.Entry("1") }));
            sb.AppendLine();
            term = p.Run(term, sb);

            term = term.Args.Last();
            term = new Term(term.GetHead(), term.Args.Concat(new[] { Atom.Parameter("v1") }));
            sb.AppendLine();
            term = p.Run(term, sb);

            term = term.Args.Last();
            term = new Term(term.GetHead(), term.Args.Concat(new[] { Atom.Entry("1") }));
            sb.AppendLine();
            term = p.Run(term, sb);
        }

        [Test]
        public void TestAdd()
        {
            var prog = Program.FromExpression<Func<int,int,int>>((i, j) => i + j);
            var sb = new StringBuilder();
            ITerm term = new Term(Atom.Entry("r4"), new[] { Atom.Entry("11"), Atom.Entry("23"), Atom.Parameter("r") });
            Assert.That(term.ToString(), Is.EqualTo("r4.11.23.r"));
            term = prog.Run(term, sb);
            Assert.That(term.ToString(), Is.EqualTo("Add.11.23.r"));
        }

        [Test]
        public void TestOrder()
        {
            var prog = Program.FromExpression<Func<Func<int,int,int>, int, int, int>>((i, j, k) => i(i(k,k),i(k,j)));
            //var term = new Term(new Atom("r18"), new[] { new Term(new Atom("Add")), new Term(new Atom("1"))
            //                                         , new Term(new Atom("2")), new Term(new Atom("r")) });
            //var sb = new StringBuilder();
            //term = prog.Run(term, sb);
            //term = term.Args.Skip(2).First();
            //term = new Term(new Atom(term.Head), term.Args.Concat(new[] { new Term(new Atom("2")) }));
            //sb.AppendLine();
            //term = prog.Run(term, sb);
        }
    }
}
