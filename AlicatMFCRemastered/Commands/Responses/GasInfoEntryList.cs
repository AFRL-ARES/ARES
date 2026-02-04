using AlicatMFCRemastered.Commands.Responses.Streamed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlicatMFCRemastered.Commands.Responses
{
  internal class GasInfoEntryList : CommandResponse
  {
    public GasInfoEntryList(char id, IEnumerable<GasInfoEntry> gasInfoEntries) : base(id)
    {
      GasInfoEntries = gasInfoEntries;
    }

    public IEnumerable<GasInfoEntry> GasInfoEntries { get; }
  }
}
