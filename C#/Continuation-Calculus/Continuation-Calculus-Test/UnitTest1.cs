using Continuation_Calculus.Version1;
using NUnit.Framework;
using System;
using System.Linq.Expressions;
using System.Text;

namespace Continuation_Calculus_Test
{
    [TestFixture]
    public class UnitTest1
    {
        private delegate T Loop<T>(Loop<T> l);

        [Test]
        public void TestMethod1()
        {
            Expression<Loop<Func<int, int>>> l = w => w(w);
            Expression<Func<Func<Func<int, int>,Func<int,int>>, Loop<Func<int, int>>>> c = 
                f => w => f(i => w(w)(i));
            var pf = Expression.Parameter(typeof(Func<Func<int, int>, Func<int, int>>), "f");
            var t1 = Expression.Invoke(c, pf);
            var t2 = Expression.Invoke(l, t1);
            var p = Program.FromExpression(Expression.Lambda<Func<Func<Func<int, int>, Func<int, int>>, Func<int, int>>>(t2, pf));
            var t = p.ToString();

            var term = new Term(new Atom("r32"), new[] { new Term(new Atom("v0")), new Term(new Atom("r")) });
            Assert.That(term.ToString(), Is.EqualTo("r32.v0.r"));
            var sb = new StringBuilder();
            term = p.Run(term, sb);
            
        }
    }
}
