using Ares.Datamodel;
using AresScript.Symbols;

namespace AresScript;

public sealed record AresExtensionFunction(
  AresValue.KindOneofCase ReceiverKind,
  string MemberName,
  AresSystemFunction Function);
