using System;
using IIM.Shared.Mediator;

namespace IIM.Application.Files;

public record IngestFileCommand(Guid FileId) : ICommand;