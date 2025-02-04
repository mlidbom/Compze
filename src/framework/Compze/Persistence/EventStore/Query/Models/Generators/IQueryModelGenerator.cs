﻿using System;
using Compze.Functional;

namespace Compze.Persistence.EventStore.Query.Models.Generators;

public interface IQueryModelGenerator;

interface IQueryModelGenerator<TDocument> : IQueryModelGenerator
{
   Option<TDocument> TryGenerate(Guid id);
}