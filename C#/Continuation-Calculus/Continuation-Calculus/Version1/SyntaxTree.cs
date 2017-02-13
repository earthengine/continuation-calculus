using Continuation_Calculus.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System;
using System.Collections.ObjectModel;

namespace Continuation_Calculus.Version1
{
    public class Atom
    {
        private readonly string name;

        public Atom(string v)
        {
            this.name = v;
        }

        public string Name { get { return name; } }

        public override bool Equals(object obj)
        {
            return (obj as Atom)?.name == name;
        }
        public override int GetHashCode()
        {
            return HashHelper.Base.HashObject(name);
        }
        public override string ToString()
        {
            return name;
        }
    }

    public class Term
    {
        private Atom head;
        private List<Term> args = new List<Term>();

        public Term(Atom h)
        {
            head = h;
        }
        public Term(Atom h, IEnumerable<Term> args)
        {
            head = h;
            this.args.AddRange(args);
        }
        public override bool Equals(object obj)
        {
            if (!(obj is Term)) return false;
            var t = obj as Term;
            return t.head.Equals(head) & Enumerable.SequenceEqual(args, t.args);
        }
        public override int GetHashCode()
        {
            return HashHelper.Base.HashObject(head)
                                  .HashEnumerable(args);
        }
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(head);
            if (args.Any())
            {
                foreach (var t in args)
                {
                    sb.Append('.');
                    if (t.args.Any())
                    {
                        sb.Append("(");
                        sb.Append(t);
                        sb.Append(")");
                    }
                    else
                    {
                        sb.Append(t);
                    }
                }
            }
            return sb.ToString();
        }

        public string Head { get { return head.Name; } }
        public IEnumerable<Term> Args { get { return args; } }
    }
    public class SimpleTerm
    {
        private readonly Atom head;
        private readonly List<Atom> args;

        public SimpleTerm(Atom h)
        {
            this.head = h;
            args = new List<Atom>();
        }

        public SimpleTerm(Atom h, IEnumerable<Atom> args)
        {
            this.head = h;
            this.args = args.ToList();
        }
        public override bool Equals(object obj)
        {
            var st = obj as SimpleTerm;
            return st != null && st.head == head &&
                Enumerable.SequenceEqual(st.args, args);
        }
        public override int GetHashCode()
        {
            return HashHelper.Base
                .HashObject(head)
                .HashEnumerable(args);
        }
        public override string ToString()
        {
            return string.Join(".", new[] { head.ToString() }.Concat(args.Select(a => a.ToString())).ToArray(), 0, args.Count + 1);
        }

        public Atom Head { get { return head; } }
        public IEnumerable<Atom> Args { get { return args; } }

        public Term Substitute(Dictionary<Atom, Term> subs)
        {
            var h = subs.ContainsKey(head) ? subs[head] : new Term(head);
            var args1 = args.Select(a => subs.ContainsKey(a) ? subs[a] : new Term(a));
            return new Term(new Atom(h.Head), h.Args.Concat(args1));
        }
    }
    public class RuleTerm
    {
        private readonly SimpleTerm mainPart;
        private readonly SimpleTerm tailPart;

        public Atom Head { get { return mainPart.Head; } }

        public RuleTerm(SimpleTerm mp)
        {
            this.mainPart = mp;
            this.tailPart = null;
        }
        public RuleTerm(SimpleTerm mp, SimpleTerm tp)
        {
            if (tp.Args.Any())
            {
                this.mainPart = mp;
                this.tailPart = tp;
            }
            else
                this.mainPart = new SimpleTerm(mp.Head, mp.Args.Concat(new[] { tp.Head }));
        }
        public override bool Equals(object obj)
        {
            var rt = obj as RuleTerm;
            return rt != null && rt.mainPart == mainPart &&
                rt.tailPart == tailPart;
        }
        public override int GetHashCode()
        {
            return HashHelper.Base
                .HashObject(mainPart)
                .HashObject(tailPart);
        }
        public override string ToString()
        {
            if (tailPart == null) return mainPart.ToString();
            else return $"{mainPart}.({tailPart})";
        }

        internal RuleTerm Rename(string oldname, string newname)
        {
            var h = mainPart.Head.Name == oldname ? new Atom(newname) : mainPart.Head;
            var args = mainPart.Args.Select(a => a.Name == oldname ? new Atom(newname) : a);
            var nmain = new SimpleTerm(h, args);
            if (tailPart != null)
            {
                h = tailPart.Head.Name == oldname ? new Atom(oldname) : tailPart.Head;
                args = tailPart.Args.Select(a => a.Name == oldname ? new Atom(newname) : a);
                return new RuleTerm(nmain, new SimpleTerm(h, args));
            }
            else return new RuleTerm(nmain);
        }

        public Term Substitute(Dictionary<Atom, Term> subs)
        {
            var mp = mainPart.Substitute(subs);
            if (tailPart == null) return mp;

            var tp = tailPart.Substitute(subs);
            return new Term(new Atom(mp.Head), mp.Args.Concat(new[] { tp }));
        }
    }
    public class Rule
    {
        private readonly SimpleTerm declare;
        private readonly RuleTerm body;

        public string RuleName { get { return declare.Head.Name; } }
        public IEnumerable<string> Args { get { return declare.Args.Select(a => a.Name); } }
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
        public Rule Rename(string oldname, string newname)
        {
            var rn = new Atom(RuleName == oldname ? newname : oldname);
            var rh = new SimpleTerm(rn, Args.Select(s => new Atom(s)));
            var rb = Body.Rename(oldname, newname);
            return new Rule(rh, rb);
        }
    }

    public class Program
    {
        private List<Rule> rules = new List<Rule>();
        private Dictionary<string, object> values = new Dictionary<string, object>();
        private List<KeyValuePair<object, string>> paramMapping1 = new List<KeyValuePair<object, string>>();
        private Rule lastRule1;

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
            lastRule1 = r;
        }
        public void RenameRules(string oldname, string newname)
        {
            rules = rules.Select(r => r.Rename(oldname, newname)).ToList();
        }
        public Program Merge(Program p2)
        {
            var p = new Program();
            p.rules.AddRange(rules);
            p.rules.AddRange(rules.Select((r, i) =>
                r.Rename(r.RuleName, $"{i}")));
            p.rules.AddRange(p2.rules.Select((r, i) => 
                r.Rename(r.RuleName, $"{i + rules.Count}")));
            p.values = values.Concat(p2.values).ToDictionary(kp => kp.Key, kp => kp.Value);
            return p;
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
            var rn = new Atom($"r{RulesCount}");
            var vr = new Atom("r");
            ClearParamMap();
            var args = forwarding.Union(conts).Select((p, i) =>
            {
                var a = new Atom($"v{i}");
                AddToParamMap(p, a.Name);
                return a;
            }).ToList();
            var nargs = Enumerable.Repeat(0, newparms).Select((r, i) => new Atom($"v{args.Count() + i}")).ToList();
            var rh = new SimpleTerm(rn, args.Concat(new[] { vr }.Concat(nargs)));
            Rule rule;
            if (cont != null)
            {
                var rb = new RuleTerm(new SimpleTerm(new Atom(handling),
                    forwarding.Select(p => new Atom(LookupParam(p)))),
                    new SimpleTerm(new Atom(cont), conts.Select(p => new Atom(LookupParam(p)))
                                                        .Concat(new[] { vr })
                                                        .Concat(nargs)));

                rule = new Rule(rh, rb);
            }
            else
            {
                var rb = new RuleTerm(new SimpleTerm(new Atom(handling), nargs.Concat(new[] { vr })));
                rule = new Rule(rh, rb);
            }
            AddRule(rule);
            return lastRule1;
        }
        private Rule AddLambdaRule(RuleConvertState rs, IEnumerable<object> nodeparams)
        {
            var a1 = rs.m1.Select(mm => mm.Key).Except(nodeparams).ToList();
            var a2 = rs.m1.Select(mm => mm.Key).Intersect(nodeparams).ToList();
            ClearParamMap();

            var args = a1.Concat(a2).Select((p, i) =>
            {
                var a = new Atom($"v{i}");
                AddToParamMap(p, a.Name);
                return a;
            }).ToList();
            var nargs = nodeparams.Except(rs.m1.Select(mm => mm.Key)).Select((p, i) => new Atom($"v{i}"));

            var vr = new Atom("r");
            var rn = new Atom($"r{RulesCount}");
            var rh = new SimpleTerm(rn, args.Concat(nargs).Concat(new[] { vr }));

            var reordered = rs.m1.Select(p => LookupParam(p.Key));
            var rb = new RuleTerm(new SimpleTerm(new Atom(rs.ru1.RuleName),
                reordered.Concat(new[] { "r" }).Select(s => new Atom(s))));
            AddRule(new Rule(rh, rb));

            rn = new Atom($"r{RulesCount}");
            rh = new SimpleTerm(rn, args.Take(a1.Count()).Concat(new[] { vr }));
            rb = new RuleTerm(new SimpleTerm(vr), new SimpleTerm(new Atom(lastRule1.RuleName), args.Take(a1.Count())));
            AddRule(new Rule(rh, rb));

            ClearParamMap();
            a1.Select((p, i) => {
                AddToParamMap(p, $"v{i}");
                return 0;
            }).ToList();

            return lastRule1;
        }

        private struct RuleConvertState{
            public Rule ru1;
            public List<KeyValuePair<object, string>> m1;
        }
        private RuleConvertState State { get { return new RuleConvertState() { ru1 = lastRule1, m1 = GetParamMap() }; } }

        private Rule AddInvocationRule(RuleConvertState exp, IEnumerable<RuleConvertState> paras)
        {
            var paral = paras.ToList();

            var rn = new Atom($"r{RulesCount}");
            var v0 = new Atom($"v0");
            var vs = paras.Select((s, i) => new Atom($"v{i + 1}")).ToList();
            var vr = new Atom("r");
            var rh = new SimpleTerm(rn, new[] { vr, v0 }.Concat(vs));
            var rb = new RuleTerm(new SimpleTerm(v0, vs.Concat(new[] { vr })));
            AddRule(new Rule(rh, rb));

            var conts = new List<object>();

            Rule lr = lastRule1;
            for (var i = paral.Count() - 1; i >= 0; --i)
            {
                lr = AddRule(paral[i].m1.Select(mm => mm.Key), conts.Distinct(), i + 1, paral[i].ru1.RuleName, lr.RuleName);
                conts.AddRange(paral[i].m1.Select(mm => mm.Key));
            }
            lr = AddRule(exp.m1.Select(mm => mm.Key), conts, 0, exp.ru1.RuleName, lr.RuleName);
            conts.AddRange(exp.m1.Select(mm => mm.Key));
            return lr;
        }
        private Rule AddParameterRule(object p)
        {
            var rn = new Atom($"r{RulesCount}");
            var v = new Atom("v0");
            var r = new Atom("r");
            var rh = new SimpleTerm(rn, new[] { v, r });
            var rb = new RuleTerm(new SimpleTerm(r), new SimpleTerm(v));
            ClearParamMap();
            AddRule(new Rule(rh, rb));
            AddToParamMap(p, v.Name);
            return lastRule1;
        }
        private Rule AddConditionalRule(RuleConvertState c, RuleConvertState t, RuleConvertState f)
        {
            var lr = AddRule(Enumerable.Empty<ParameterExpression>(), Enumerable.Empty<ParameterExpression>(), 3, "Conditional", "r");
            lr = AddRule(c.m1.Select(mm => mm.Key).Distinct(), Enumerable.Empty<ParameterExpression>(), 2, c.ru1.RuleName, lr.RuleName);
            lr = AddRule(t.m1.Select(mm => mm.Key).Distinct(), c.m1.Select(mm => mm.Key).Distinct(), 1, t.ru1.RuleName, lr.RuleName);
            return AddRule(f.m1.Select(mm => mm.Key).Distinct(), t.m1.Select(mm => mm.Key).Union(c.m1.Select(mm => mm.Key)).Distinct(), 0, 
                f.ru1.RuleName, lr.RuleName);
        }
        private Rule AddBinaryRule(RuleConvertState r, RuleConvertState l, string rule)
        {
            var lr = AddRule(Enumerable.Empty<ParameterExpression>(), Enumerable.Empty<ParameterExpression>(), 2, rule, null);

            lr = AddRule(r.m1.Select(mm => mm.Key).Distinct(), Enumerable.Empty<ParameterExpression>(), 1, r.ru1.RuleName, lr.RuleName);

            return AddRule(l.m1.Select(mm => mm.Key).Distinct(), r.m1.Select(mm => mm.Key).Distinct(), 0, l.ru1.RuleName, lr.RuleName);
        }
        private Rule AddUnaryRule(RuleConvertState o, string rule)
        {
            var lr = AddRule(Enumerable.Empty<ParameterExpression>(), Enumerable.Empty<ParameterExpression>(), 2, rule, null);

            return AddRule(o.m1.Select(mm => mm.Key).Distinct(), Enumerable.Empty<ParameterExpression>(), 1, o.ru1.RuleName, lr.RuleName);
        }
        private Rule AddConstantRule(string rule)
        {
            return AddRule(Enumerable.Empty<ParameterExpression>(), Enumerable.Empty<ParameterExpression>(), 0, rule, null);
        }

        public static Program FromExpression<T>(Expression<T> ex)
        {
            var r = new ProgramVisitor();
            var e = r.Visit(ex.Body);
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
                var paras = node.Arguments.Select(e => {
                    Visit(e);
                    return new { prog.State, e };
                }).ToList();

                var result = Expression.Invoke(rexp, paras.Select(p => p.e));

                var lr = prog.AddInvocationRule(st, paras.Select(p => p.State));

                return result;
            }
        }
    }

    public static class Programs
    {
        public class InsuficiantArgumentsException : Exception
        {

        }
        public class ArgumentOverflowException : Exception
        {

        }

        public static Term Eval(this Program p, Term t)
        {
            var r = p.FindRule(t.Head);
            if (r == null)
                return t;
            if (t.Args.Count() > r.Args.Count())
                throw new ArgumentOverflowException();
            else if (t.Args.Count() < r.Args.Count())
                throw new InsuficiantArgumentsException();

            var dict = r.Args.Zip(t.Args, (s, te) =>
                new { s, te }).ToDictionary(ste => new Atom(ste.s), ste => ste.te);
            return r.Body.Substitute(dict);            
        }
        public static Term Run(this Program p, Term t, StringBuilder sb)
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
    }
}
