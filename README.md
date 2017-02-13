# Continuation Calculus
[Continuation Calculus](https://arxiv.org/abs/1309.1257?context=cs) is an elegant programming language
with minimum language structure, but still have the ability to express complicated algorithms.

The purpose of this project is to examine its ability.  

## Atoms
In continuation calculus, an atom refer to a value only understandable by 
external or a continuation to the other part of the code.

We didn't define the language for parsing yet. But we assume that any constructs
that does not part of a higher level of constructs fall back to part of Atoms.

The followings are atoms:

```
v0
r
Add
"Hello world"
```


## Terms
A term starts with an atom, following by zero or more terms.

```
Term :: Atom | Term "." Atom | Term ".(" Term ")"
```

The followings are Terms (note we use :

```
r0.v0.v1.r
r1.(v0.v2.r)
r2.(v1.v2).(v3.v4)
Add.1.(Add.2.3).r
```

### Simple Terms
Simple terms are terms where all sub-terms are atoms.

```
SimpleTerm :: Atom | Atom "." SimpleTerm

### Rule Term
A rule term is either a simple term, or its last term is a simple term.

```
RuleTerm :: SimpleTerm | SimpleTerm ".(" SimpleTerm ")"
```

## Rules
A rule is a combination of a declaration, which is a simple term, and a definition,
which is a rule term.

## Program
A set of rules, some of them are named and marked to be available to external code.



