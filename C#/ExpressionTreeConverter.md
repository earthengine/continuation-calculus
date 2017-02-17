# Expression Tree Converter
To create program code written in Continuation Calculus, we can manually write code.
However, we do not want to start with a parser. Instead, we write converters
to convert code written in other languages into it. Expression tree is a elegant sub-
language in C#, and C# provides a good API to handle its structure. So we start with this.

## Limitations
We only support a subset of available Expression Tree structures. Namely, the following
structures are supported:

* Binary operators
* Unary operators
* TypeIs operator treated as unary
* Conditional operator (?:)
* Constants
* Lambdas
* Parameters
* Invocations

And the following structures are not supported yet:

* Method calls (may add in later steps)
* New operator (including member init, list init etc)

# Algorithm
The current implementation is a Call-By-Value flavor. It is possible to move to Call-By-Name.
However in CC it looks like the natual behaviour is more similar to Call-By-Name rather than Call-By-Value.
So Call-By-Name conversion would be much easier. But we still need to check this out.

## Parameters
Each parameter get converted into a simgle rule like the following:

```
rule.v0.r -> r.v0
```

## Invocations
If we have an Expression Tree node 

```
Invoke(exp1(p0,p1,p2), exp2(p0), exp3(p2,p4))
```

and we have already converted

```
...(rules for exp1)
rexp1.v0.v1.v2.r -> ...
...(rules for exp2)
rexp2.v0.r -> ...
...(rules for exp3)
rexp3.v0.v1.r -> ...
```

Then the invoke node converted to

```
ri0.r.v0.v1.v2 -> v0.v1.v2.r
ri1.v0.v1.r.v2.v3 -> rexp3.v0.v1.(ri0.v2.v3)
ri2.v0.v1.v2.r.v3 -> rexp2.v0.(ri1.v1.v2.r.v3)
rinvoke.v0.v1.v2.v3.r -> rexp1.v0.v1.v2.(ri2.v0.v1.v3)
```

* `ri0` receives all evaluated values of the 3 expressions.
* Assuming `exp1` and `exp2` being calculated, `ri1` evaluates `exp3`, pass it to `ri0` with calculated values
* You can see that the position of `r` was moved from left to right. 
* Once it is in the rightmost position, we have done.

## Lambdas

A typical lambda

()