using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Continuation_Calculus.Utils
{
    public static class ExpressionHelper
    {
        private delegate T Loop<T>(Loop<T> l);

        public static IEnumerable<ParameterExpression> GetDelegateParameters<D>()
        {
            var m = typeof(D).GetTypeInfo().GetDeclaredMethod("Invoke");
            foreach (var pi in m.GetParameters())
            {
                yield return Expression.Parameter(pi.ParameterType);
            }
        }

        public static Expression<R> Apply<T,R>(Expression<Func<T,R>> t, Expression<T> v)
        {
            var ps = GetDelegateParameters<R>().ToArray();

            return Gamma<R>(Expression.Invoke(t, v));
        }
        public static Expression<R> Gamma<R>(Expression e)
        {
            var ps = GetDelegateParameters<R>().ToArray();
            return Expression.Lambda<R>(Expression.Invoke(e, ps), ps);
        }

        public static Expression<Func<T, R>> Compose<T, M, R>(Expression<Func<M, R>> f, Expression<Func<T, M>> g)
        {
            var p = Expression.Parameter(typeof(T), "p");
            return Expression.Lambda<Func<T, R>>(Expression.Invoke(f, Expression.Invoke(g, p)), p);
        }

        public static Expression<Func<Func<F, F>, F>> YExp<F>()
        {
            var pf = Expression.Parameter(typeof(Func<F,F>), "f");
            var pw = Expression.Parameter(typeof(Loop<F>), "w");

            var e = Expression.Lambda<Func<Func<F, F>, Loop<F>>>(
                Expression.Lambda<Loop<F>>(Expression.Invoke(pf, Gamma<F>(Expression.Invoke(pw, pw))), pw), pf);
            return Compose(w => w(w), e);
        }

        public static Expression<Func<Func<Func<T, R>, Func<T, R>>, Func<T, R>>> YExp<T, R>()
        {
            return YExp<Func<T, R>>();
            //Expression<Func<Func<Func<T, R>, Func<T, R>>, Loop<Func<T, R>>>> e =
                //f => w => f(t => w(w)(t));
            //return Compose(w => w(w), e);
        }
    }
}
