# Continuation Calculus
[Continuation Calculus](https://arxiv.org/abs/1309.1257?context=cs) is an elegant programming language
with minimum language structure, but still have the ability to express complicated algorithms.


## Atoms
In continuation calculus, an atom refer to a value only understandable by 
external or a continuation to the other part of the code.

## Terms
A term starts with an atom, following by zero or more terms.

### Simple Terms
Simple terms are terms where all sub-terms are atoms.

### Rule Term
A rule term is either a simple term, or its last term is a simple term.

## Rules
A rule is a combination of a declaration, which is a simple term, and a definition,
which is a rule term.

## Program
A set of rules, some of them are named and marked to be available to external code.



