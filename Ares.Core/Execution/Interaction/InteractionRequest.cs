using System;
using System.Collections.Generic;
using System.Text;

namespace Ares.Core.Execution.Interaction;

public record InteractionRequest(string RequestId, InteractionType Type, string Prompt);
