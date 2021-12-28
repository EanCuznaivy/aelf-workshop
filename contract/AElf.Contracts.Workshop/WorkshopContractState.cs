using AElf.Sdk.CSharp.State;
using AElf.Types;
using AElf.Contracts.MultiToken;

namespace AElf.Contracts.Workshop
{
    public class WorkshopContractState : ContractState
    {
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        public MappedState<long, Workshop> WorkshopMap { get; set; }
        public Int64State NextWorkshop { get; set; }
        public SingletonState<Address> Owner { get; set; }
    }
}