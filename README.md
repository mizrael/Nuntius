# Nuntius

> *Nuntius* (Latin): *messenger*

**Nuntius** is a small, opinionated, open‑source C# library inspired by [MediatR](https://github.com/LuckyPennySoftware/MediatR/), designed to cover only a **minimal, carefully chosen subset** of its features.

## What Nuntius Is

* A **simple mediator / message dispatcher**
* Focused on the most common use cases
* Easy to read, debug, and extend
* Designed to feel at home in modern C# codebases

## What Nuntius Is Not

* A full MediatR replacement
* A plugin‑heavy or pipeline‑driven system

Nuntius deliberately implements **only a small subset of MediatR concepts**, and this scope is intentional and permanent.

## When to Use Nuntius

Nuntius is a good fit if you:

* Like the mediator pattern
* Are looking for a simple way to implement the CQRS pattern
* Want fewer abstractions
* Prefer small, understandable libraries
* Don’t need every MediatR feature

If you need advanced pipelines, behaviors, or extensive extension points, MediatR is likely the better choice.

If you need a full message bus with support for sagas and message persistence, [OpenSleigh](https://github.com/mizrael/OpenSleigh/) can be a good alternative.

