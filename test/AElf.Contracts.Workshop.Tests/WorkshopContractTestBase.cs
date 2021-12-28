using AElf.Boilerplate.TestBase;
using AElf.Contracts.Association;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Profit;
using AElf.Contracts.TokenConverter;
using AElf.Contracts.Vote;
using AElf.Cryptography.ECDSA;
using AElf.EconomicSystem;
using AElf.GovernmentSystem;
using AElf.Types;

namespace AElf.Contracts.Workshop
{
    public class WorkshopContractTestBase : DAppContractTestBase<WorkshopContractTestModule>
    {
        // You can get address of any contract via GetAddress method, for example:
        // internal Address DAppContractAddress => GetAddress(DAppSmartContractAddressNameProvider.StringName);
        internal Address VoteContractAddress => GetAddress(VoteSmartContractAddressNameProvider.StringName);
        internal Address ProfitContractAddress => GetAddress(ProfitSmartContractAddressNameProvider.StringName);

        internal Address AssociationContractAddress =>
            GetAddress(AssociationSmartContractAddressNameProvider.StringName);

        internal WorkshopContractContainer.WorkshopContractStub GetWorkshopContractStub(ECKeyPair senderKeyPair)
        {
            return GetTester<WorkshopContractContainer.WorkshopContractStub>(DAppContractAddress, senderKeyPair);
        }

        internal TokenContractContainer.TokenContractStub GetTokenContractStub(ECKeyPair senderKeyPair)
        {
            return GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, senderKeyPair);
        }

        internal VoteContractContainer.VoteContractStub GetVoteContractStub(ECKeyPair senderKeyPair)
        {
            return GetTester<VoteContractContainer.VoteContractStub>(VoteContractAddress, senderKeyPair);
        }

        internal ProfitContractContainer.ProfitContractStub GetProfitContractStub(ECKeyPair senderKeyPair)
        {
            return GetTester<ProfitContractContainer.ProfitContractStub>(ProfitContractAddress, senderKeyPair);
        }

        internal AssociationContractImplContainer.AssociationContractImplStub GetAssociationContractStub(
            ECKeyPair senderKeyPair)
        {
            return GetTester<AssociationContractImplContainer.AssociationContractImplStub>(AssociationContractAddress,
                senderKeyPair);
        }
    }
}