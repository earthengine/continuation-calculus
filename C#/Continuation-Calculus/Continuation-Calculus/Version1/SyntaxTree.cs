using Continuation_Calculus.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System;

namespace Continuation_Calculus.Version1
{
    public class Rule
    {
        private readonly SimpleTerm declare;
        private readonly RuleTerm body;

        public string RuleName { get { return declare.Head; } }
        public IEnumerable<string> Args { get { return declare.Args.Select(a => a.Head); } }
        public RuleTerm Body { get { return body; } }

        public Rule(SimpleTerm st, RuleTerm rt)
        {
            this.declare = st;
            this.body = rt;
        }
        public override bool Equals(object obj)
        {
            var r = obj as Rule;
            return r != null && r.declare == declare && r.body == body;
        }
        public override int GetHashCode()
        {
            return HashHelper.Base
                .HashObject(declare)
                .HashObject(body);
        }
        public override string ToString()
        {
            return declare.ToString() + " -> " + body.ToString();
        }
    }

    public class Program
    {
        private List<Rule> rules = new List<Rule>();
        private Dictionary<string, object> values = new Dictionary<string, object>();
        private List<KeyValuePair<object, string>> paramMapping1 = new List<KeyValuePair<object, string>>();
        private Rule lastRule;

        public Rule FindRule(string name)
        {
            return rules.FirstOrDefault(r => r.RuleName == name);
        }

        public int RulesCount { get { return rules.Count; } }

        internal string AddValue(object value)
        {
            var name = $"v{values.Count}";
            values.Add(name, value);
            return name;
        }

        internal void AddRule(Rule r)
        {
            rules.Add(r);
            lastRule = r;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var r in rules)
            {
                sb.AppendLine(r.ToString());
            }
            return sb.ToString();
        }

        private void ClearParamMap()
        {
            paramMapping1.Clear();
        }
        private void AddToParamMap(object p, string n)
        {
            paramMapping1.Add(new KeyValuePair<object, string>(p, n));
        }
        private string LookupParam(object p)
        {
            return paramMapping1.ToDictionary(pp => pp.Key, pp => pp.Value)[p];
        }
        private List<KeyValuePair<object, string>> GetParamMap()
        {
            return new List<KeyValuePair<object, string>>(paramMapping1);
        }


        private Rule AddRule(IEnumerable<object> forwarding, IEnumerable<object> conts, int newparms, string handling, string cont)
        {
            var rn = Atom.Entry($"r{RulesCount}");
            var vr = Atom.Parameter("r");
            ClearParamMap();
            var args = forwarding.Union(conts).Select((p, i) =>
            {
                var a = Atom.Parameter($"v{i}");
                AddToParamMap(p, a.Name);
                return a;
            }).ToList();
            var nargs = Enumerable.Repeat(0, newparms).Select((r, i) => Atom.Parameter($"v{args.Count() + i}")).ToList();
            var rh = new SimpleTerm(rn, args.Concat(new[] { vr }.Concat(nargs)));
            Rule rule;
            if (cont != null)
            {
                var rb = new RuleTerm(new SimpleTerm(Atom.Entry(handling),
                    forwarding.Select(p => Atom.Parameter(LookupParam(p)))),
                    new SimpleTerm(Atom.Entry(cont), conts.Distinct()
                                                        .Select(p => Atom.Parameter(LookupParam(p)))
                                                        .Concat(new[] { vr })
                                                        .Concat(nargs)));

                rule = new Rule(rh, rb);
            }
            else
            {
                var rb = new RuleTerm(new SimpleTerm(Atom.Entry(handling), nargs.Concat(new[] { vr })));
                rule = new Rule(rh, rb);
            }
            AddRule(rule);
            return lastRule;
        }
        private Rule AddLambdaRule(RuleConvertState rs, IEnumerable<object> nodeparams)
        {
            var a1 = rs.paramMap.Select(mm => mm.Key).Except(nodeparams).ToList();
            var a2 = nodeparams.Intersect(rs.paramMap.Select(mm => mm.Key)).ToList();
            ClearParamMap();

            var args = a1.Concat(a2).Select((p, i) =>
            {
                var a = Atom.Parameter($"v{i}");
                AddToParamMap(p, a.Name);
                return a;
            }).ToList();
            var nargs = nodeparams.Except(rs.paramMap.Select(mm => mm.Key)).Select((p, i) => Atom.Parameter($"v{i}"));

            var vr = Atom.Parameter("r");
            var rn = Atom.Entry($"r{RulesCount}");
            var rh = new SimpleTerm(rn, args.Concat(nargs).Concat(new[] { vr }));

            var reordered = rs.paramMap.Select(p => LookupParam(p.Key));
            var rb = new RuleTerm(new SimpleTerm(Atom.Entry(rs.rule.RuleName),
                reordered.Concat(new[] { "r" }).Select(s => Atom.Parameter(s))));
            AddRule(new Rule(rh, rb));

            rn = Atom.Entry($"r{RulesCount}");
            rh = new SimpleTerm(rn, args.Take(a1.Count()).Concat(new[] { vr }));
            rb = new RuleTerm(new SimpleTerm(vr), new SimpleTerm(Atom.Entry(lastRule.RuleName), args.Take(a1.Count())));
            AddRule(new Rule(rh, rb));

            ClearParamMap();
            a1.Select((p, i) =>
            {
                AddToParamMap(p, $"v{i}");
                return 0;
            }).ToList();

            return lastRule;
        }

        private struct RuleConvertState
        {
            public Expression converting;
            public Rule rule;
            public List<KeyValuePair<object, string>> paramMap;
        }
        private RuleConvertState State
        {
            get
            {
                return new RuleConvertState()
                {
                    rule = lastRule,
                    paramMap = GetParamMap()
                };
            }
        }

        private Rule AddInvocationRule(RuleConvertState exp, IEnumerable<RuleConvertState> paras)
        {
            var paral = paras.ToList();

            var rn = Atom.Entry($"r{RulesCount}");
            var v0 = Atom.Parameter($"v0");
            var vs = paras.Select((s, i) => Atom.Parameter($"v{i + 1}")).ToList();
            var vr = Atom.Parameter("r");
            var rh = new SimpleTerm(rn, new[] { vr, v0 }.Concat(vs));
            var rb = new RuleTerm(new SimpleTerm(v0, vs.Concat(new[] { vr })));
            AddRule(new Rule(rh, rb));

            var conts = new List<object>();

            Rule lr = lastRule;
            for (var i = paral.Count() - 1; i >= 0; --i)
            {
                lr = AddRule(paral[i].paramMap.Select(mm => mm.Key), conts, i + 1, paral[i].rule.RuleName, lr.RuleName);
                conts = paramMapping1.Select(mm => mm.Key).ToList();
            }
            lr = AddRule(exp.paramMap.Select(mm => mm.Key), conts, 0, exp.rule.RuleName, lr.RuleName);
            conts.AddRange(exp.paramMap.Select(mm => mm.Key));
            return lr;
        }
        private Rule AddParameterRule(object p)
        {
            var rn = Atom.Entry($"r{RulesCount}");
            var v = Atom.Parameter("v0");
            var r = Atom.Parameter("r");
            var rh = new SimpleTerm(rn, new[] { v, r });
            var rb = new RuleTerm(new SimpleTerm(r), new SimpleTerm(v));
            ClearParamMap();
            AddRule(new Rule(rh, rb));
            AddToParamMap(p, v.Name);
            return lastRule;
        }
        private Rule AddConditionalRule(RuleConvertState c, RuleConvertState t, RuleConvertState f)
        {
            var rn = Atom.Entry($"r{RulesCount}");
            IEnumerable<Atom> args = new[] { Atom.Parameter("rt"), Atom.Parameter("rf"), Atom.Parameter("c") };
            var rh = new SimpleTerm(rn, args);
            var rb = new RuleTerm(new SimpleTerm(Atom.Entry("If"), args.Skip(2).Concat(args.Take(2))));
            var lr = new Rule(rh, rb);
            AddRule(lr);

            //TODO: Implement this
            throw new NotImplementedException();

        }
        private Rule AddBinaryRule(RuleConvertState l, RuleConvertState r, string rule)
        {
            var lr = AddRule(Enumerable.Empty<ParameterExpression>(), Enumerable.Empty<ParameterExpression>(), 2, rule, null);

            lr = AddRule(r.paramMap.Select(mm => mm.Key), Enumerable.Empty<ParameterExpression>(), 1, r.rule.RuleName, lr.RuleName);

            return AddRule(l.paramMap.Select(mm => mm.Key), r.paramMap.Select(mm => mm.Key).Distinct(), 0, l.rule.RuleName, lr.RuleName);
        }
        private Rule AddUnaryRule(RuleConvertState o, string rule)
        {
            var lr = AddRule(Enumerable.Empty<ParameterExpression>(), Enumerable.Empty<ParameterExpression>(), 2, rule, null);

            return AddRule(o.paramMap.Select(mm => mm.Key), Enumerable.Empty<ParameterExpression>(), 1, o.rule.RuleName, lr.RuleName);
        }
        private Rule AddConstantRule(string rule)
        {
            return AddRule(Enumerable.Empty<ParameterExpression>(), Enumerable.Empty<ParameterExpression>(), 0, rule, null);
        }

        public static Program FromExpression<T>(Expression<T> ex)
        {
            var r = new ProgramVisitor();
            var e = r.Visit(ex);
            return r.prog;
        }

        private class ProgramVisitor : ExpressionVisitor
        {
            public Program prog { get; } = new Program();

            private Expression BuildBinaryRule(RuleConvertState stl, RuleConvertState str, string rule, Expression result)
            {
                prog.AddBinaryRule(stl, str, rule);
                return result;
            }
            private Expression BuildUnaryRule(RuleConvertState st, string rule, Expression result)
            {
                prog.AddUnaryRule(st, rule);
                return result;
            }

            protected override Expression VisitBinary(BinaryExpression node)
            {
                var l = Visit(node.Left);
                var stl = prog.State;
                var r = Visit(node.Right);
                var str = prog.State;

                switch (node.NodeType)
                {
                    case ExpressionType.Add:
                        return BuildBinaryRule(stl, str, "Add", Expression.Add(l, r));
                    case ExpressionType.AddChecked:
                        return BuildBinaryRule(stl, str, "AddChecked", Expression.AddChecked(l, r));
                    case ExpressionType.Divide:
                        return BuildBinaryRule(stl, str, "Devide", Expression.Divide(l, r));
                    case ExpressionType.Modulo:
                        return BuildBinaryRule(stl, str, "Modulo", Expression.Modulo(l, r));
                    case ExpressionType.Multiply:
                        return BuildBinaryRule(stl, str, "Multiply", Expression.Multiply(l, r));
                    case ExpressionType.MultiplyChecked:
                        return BuildBinaryRule(stl, str, "MultiplyChecked", Expression.MultiplyChecked(l, r));
                    case ExpressionType.Power:
                        return BuildBinaryRule(stl, str, "Power", Expression.Power(l, r));
                    case ExpressionType.Subtract:
                        return BuildBinaryRule(stl, str, "Subtract", Expression.Subtract(l, r));
                    case ExpressionType.SubtractChecked:
                        return BuildBinaryRule(stl, str, "SubtractChecked", Expression.SubtractChecked(l, r));

                    case ExpressionType.And:
                        return BuildBinaryRule(stl, str, "And", Expression.And(l, r));
                    case ExpressionType.AndAlso:
                        return BuildBinaryRule(stl, str, "AndAlso", Expression.AndAlso(l, r));
                    case ExpressionType.ExclusiveOr:
                        return BuildBinaryRule(stl, str, "ExclusiveOr", Expression.ExclusiveOr(l, r));
                    case ExpressionType.Or:
                        return BuildBinaryRule(stl, str, "Or", Expression.Or(l, r));
                    case ExpressionType.OrElse:
                        return BuildBinaryRule(stl, str, "OrElse", Expression.OrElse(l, r));

                    case ExpressionType.ArrayIndex:
                        return BuildBinaryRule(stl, str, "ArrayIndex", Expression.ArrayIndex(l, r));
                    case ExpressionType.Coalesce:
                        return BuildBinaryRule(stl, str, "Coalesce", Expression.Coalesce(l, r));

                    case ExpressionType.Equal:
                        return BuildBinaryRule(stl, str, "Equal", Expression.Equal(l, r));
                    case ExpressionType.GreaterThan:
                        return BuildBinaryRule(stl, str, "GreaterThan", Expression.GreaterThan(l, r));
                    case ExpressionType.GreaterThanOrEqual:
                        return BuildBinaryRule(stl, str, "GreaterThanOrEqual", Expression.GreaterThanOrEqual(l, r));
                    case ExpressionType.LessThan:
                        return BuildBinaryRule(stl, str, "LessThan", Expression.LessThan(l, r));
                    case ExpressionType.LessThanOrEqual:
                        return BuildBinaryRule(stl, str, "LessThanOrEqual", Expression.LessThanOrEqual(l, r));
                    case ExpressionType.NotEqual:
                        return BuildBinaryRule(stl, str, "Negate", Expression.NotEqual(l, r));

                    case ExpressionType.LeftShift:
                        return BuildBinaryRule(stl, str, "LeftShift", Expression.LeftShift(l, r));
                    case ExpressionType.RightShift:
                        return BuildBinaryRule(stl, str, "RightShift", Expression.RightShift(l, r));
                    default:
                        throw new NotImplementedException();
                }
            }
            protected override Expression VisitUnary(UnaryExpression node)
            {
                var r = Visit(node.Operand);
                var st = prog.State;
                switch (node.NodeType)
                {
                    case ExpressionType.ArrayLength:
                        return BuildUnaryRule(st, "ArrayLength", Expression.ArrayLength(r));
                    case ExpressionType.Convert:
                        return Expression.Convert(r, node.Type);
                    case ExpressionType.ConvertChecked:
                        return Expression.Convert(r, node.Type);
                    case ExpressionType.Negate:
                        return BuildUnaryRule(st, "Negate", Expression.Negate(r));
                    case ExpressionType.UnaryPlus:
                        return BuildUnaryRule(st, "UnaryPlus", Expression.UnaryPlus(r));
                    case ExpressionType.NegateChecked:
                        return BuildUnaryRule(st, "NegateChecked", Expression.NegateChecked(r));
                    case ExpressionType.Not:
                        return BuildUnaryRule(st, "Not", Expression.Not(r));
                    case ExpressionType.Quote:
                        return BuildUnaryRule(st, "Quote", Expression.Quote(r));
                    case ExpressionType.TypeAs:
                        return Expression.TypeAs(r, node.Type);
                    default:
                        throw new NotImplementedException();
                }
            }
            protected override Expression VisitTypeBinary(TypeBinaryExpression node)
            {
                var r = base.Visit(node);
                return BuildUnaryRule(prog.State, $"TypeIs{node.TypeOperand}", r);
            }
            protected override Expression VisitConditional(ConditionalExpression node)
            {
                var rcond = Visit(node.Test);
                var stc = prog.State;
                var rt = Visit(node.IfTrue);
                var stt = prog.State;
                var rf = Visit(node.IfFalse);
                var stf = prog.State;

                prog.AddConditionalRule(stc, stt, stf);

                return Expression.Condition(rcond, rt, rf);
            }

            protected override Expression VisitConstant(ConstantExpression node)
            {
                prog.AddConstantRule(node.Value.ToString());
                return base.VisitConstant(node);
            }
            protected override Expression VisitLambda<T>(Expression<T> node)
            {
                var r = Visit(node.Body);

                prog.AddLambdaRule(prog.State, node.Parameters);

                return Expression.Lambda<T>(r, node.Parameters);
            }
            protected override Expression VisitParameter(ParameterExpression node)
            {
                prog.AddParameterRule(node);
                return base.VisitParameter(node);
            }
            protected override Expression VisitInvocation(InvocationExpression node)
            {
                var rexp = Visit(node.Expression);
                var st = prog.State;
                var paras = node.Arguments.Select(e =>
                {
                    Visit(e);
                    return new { prog.State, e };
                }).ToList();

                var result = Expression.Invoke(rexp, paras.Select(p => p.e));

                var lr = prog.AddInvocationRule(st, paras.Select(p => p.State));

                return result;
            }
        }
    }

    public static class ProgramExecution
    {
        public class InsuficiantArgumentsException : Exception
        {

        }
        public class ArgumentOverflowException : Exception
        {

        }

        public static ITerm Eval(this Program p, ITerm t)
        {
            var r = p.FindRule(t.Head);
            if (r == null)
                return t;
            if (t.Args.Count() > r.Args.Count())
                throw new ArgumentOverflowException();
            else if (t.Args.Count() < r.Args.Count())
                throw new InsuficiantArgumentsException();

            var dict = r.Args.Zip(t.Args, (s, te) =>
                new { s, te }).ToDictionary(ste => Atom.Parameter(ste.s), ste => ste.te);
            return r.Body.Subsitute(dict);
        }
        public static ITerm Run(this Program p, ITerm t, StringBuilder sb)
        {
            sb.AppendLine(t.ToString());
            for (;;)
            {
                var nt = p.Eval(t);
                sb.Append("=>");
                sb.AppendLine(nt.ToString());
                if (nt == t) return t;
                t = nt;
            }
        }
        public static ITerm Run(this Program p, ITerm t)
        {
            for (;;)
            {
                var nt = p.Eval(t);
                if (nt == t) return t;
                t = nt;
            }
        }
        public static void EvalRule(this Program p, ITerm t)
        {
            var r = p.FindRule(t.Head);
            if (r == null) return;

            var pars = t.GetAtoms().Where(a => a.HeadIsParameter).Distinct();
            var nps = r.Args.Count() - t.Args.Count();
            if (nps < 0) throw new ArgumentOverflowException();
            var npars = Enumerable.Repeat(0, nps).Select((_, i) => Atom.Parameter($"v{pars.Count() + i}"));

            ITerm nt = new Term(t.GetHead(), t.Args.Concat(npars));
            nt = p.Run(nt);

            var rn = Atom.Entry($"r{p.RulesCount}");


        }
    }
}
