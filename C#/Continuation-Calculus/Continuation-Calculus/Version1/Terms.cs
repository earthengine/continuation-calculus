using Continuation_Calculus.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Continuation_Calculus.Version1
{
    public interface ITerm
    {
        string Head { get; }
        bool HeadIsParameter { get; }
        IEnumerable<ITerm> Args { get; }
    }
    public static class Terms
    {
        public static Atom GetHead(this ITerm t)
        {
            return t.HeadIsParameter ? Atom.Parameter(t.Head) : Atom.Entry(t.Head);
        }
        public static IEnumerable<Atom> GetAtoms(this ITerm t)
        {
            return new[] { t.GetHead() }.Concat(t.Args.SelectMany(tt => tt.GetAtoms()));
        }
        public static IEnumerable<Atom> UsedParameters(this ITerm t)
        {
            return t.GetAtoms().Where(a => a.HeadIsParameter).Distinct();
        }
        public static ITerm Subsitute(this ITerm t, Dictionary<Atom, ITerm> subs)
        {
            if (subs.ContainsKey(t.GetHead()))
            {
                var th = subs[t.GetHead()];
                return new Term(th.GetHead(), th.Args.Concat(t.Args.Select(a => a.Subsitute(subs))));
            }
            else
            {
                return new Term(t.GetHead(), t.Args.Select(a => a.Subsitute(subs)));
            }
        }
    }

    public class Atom : ITerm
    {
        private readonly string name;
        private bool parameter = false;

        private Atom(string v)
        {
            this.name = v;
        }
        public static Atom Entry(string s)
        {
            return new Atom(s);
        }
        public static Atom Parameter(string s)
        {
            return new Atom(s) { parameter = true };
        }

        public bool HeadIsParameter { get { return parameter; } }

        public string Name { get { return name; } }

        public string Head
        {
            get
            {
                return name;
            }
        }

        public IEnumerable<ITerm> Args
        {
            get
            {
                return Enumerable.Empty<ITerm>();
            }
        }

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
    public class Term : ITerm
    {
        private Atom head;
        private List<ITerm> args = new List<ITerm>();

        public Term(Atom h)
        {
            head = h;
        }
        public Term(Atom h, IEnumerable<ITerm> args)
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
                    if (t.Args.Any())
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
        public IEnumerable<ITerm> Args { get { return args; } }

        public bool HeadIsParameter
        {
            get
            {
                return head.HeadIsParameter;
            }
        }
    }
    public class SimpleTerm : ITerm
    {
        private readonly Atom head;
        private readonly List<Atom> args;

        public IEnumerable<Atom> Atoms { get { return new[] { head }.Concat(args); } }

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

        public string Head { get { return head.Name; } }
        public IEnumerable<ITerm> Args { get { return args; } }

        public bool HeadIsParameter
        {
            get
            {
                return head.HeadIsParameter;
            }
        }
    }
    public class RuleTerm : ITerm
    {
        private readonly SimpleTerm mainPart;
        private readonly SimpleTerm tailPart;

        public string Head { get { return mainPart.Head; } }

        public IEnumerable<ITerm> Args
        {
            get
            {
                return mainPart.Args.Concat(tailPart == null ? Enumerable.Empty<ITerm>() : new[] { tailPart });
            }
        }

        public bool HeadIsParameter
        {
            get
            {
                return mainPart.HeadIsParameter;
            }
        }

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
                this.mainPart = new SimpleTerm(Atom.Entry(mp.Head), mp.Args.Select(a => a as Atom).Concat(new[] { Atom.Entry(tp.Head) }));
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

    }
}
