using AElf.CSharp.Core;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Workshop
{
    public partial class WorkshopContract
    {
        public override Workshop GetCurrentWorkshop(Empty input)
        {
            var currentWorkshopId = State.NextWorkshop.Value.Sub(1);
            return State.WorkshopMap[currentWorkshopId];
        }

        public override Workshop GetWorkshop(Int64Value input)
        {
            return State.WorkshopMap[input.Value];
        }
    }
}