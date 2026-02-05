using Ares.Datamodel;

namespace AresScript;

public sealed record AresExtensionFunction(
  AresValue.KindOneofCase ReceiverKind,
  string MemberName,
  AresSystemFunction Function);
