using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Workshop
{
    public partial class WorkshopContract : WorkshopContractContainer.WorkshopContractBase
    {
        public override HelloReturn Hello(HelloInput input)
        {
            Assert(State.Owner.Value == null, "Already initialized.");
            State.Owner.Value = input.Owner ?? Context.Sender;
            State.NextWorkshop.Value = 1;

            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            Context.Fire(new WorkshopsStarted
            {
                Name = input.Name
            });
            return new HelloReturn {Value = "Workshops started."};
        }

        public override Empty StartWorkshop(StartWorkshopInput input)
        {
            AssertSenderIsOwner();
            var currentWorkshopId = State.NextWorkshop.Value;
            var workshop = new Workshop
            {
                Title = input.Title,
                StartTime = Context.CurrentBlockTime,
                Id = currentWorkshopId
            };
            State.NextWorkshop.Value = State.NextWorkshop.Value.Add(1);
            var balance = State.TokenContract.GetBalance.Call(new GetBalanceInput
            {
                Owner = State.Owner.Value,
                Symbol = Context.Variables.NativeSymbol
            }).Balance;
            workshop.StartBalance = balance;
            State.WorkshopMap[currentWorkshopId] = workshop;
            return new Empty();
        }

        public override Empty EndWorkshop(EndWorkshopInput input)
        {
            AssertSenderIsOwner();
            var onGoingWorkshopId = State.NextWorkshop.Value.Sub(1);
            var workshop = State.WorkshopMap[onGoingWorkshopId];
            workshop.EndTime = Context.CurrentBlockTime;
            var balance = State.TokenContract.GetBalance.Call(new GetBalanceInput
            {
                Owner = State.Owner.Value,
                Symbol = Context.Variables.NativeSymbol
            }).Balance;
            workshop.EndBalance = balance;
            State.WorkshopMap[onGoingWorkshopId] = workshop;
            return new Empty();
        }

        public override Empty ResetOwner(Address input)
        {
            var organizationAddress = Address.FromBase58("2Brtd6sY7KV8oehxM6YeNMYXzxYMXWHfsSQxQwoFHwYPrLynV2");
            Assert(Context.Sender == organizationAddress, "No permission.");
            State.Owner.Value = input;
            return new Empty();
        }

        public override Address GetOwner(Empty input)
        {
            return State.Owner.Value;
        }

        private void AssertSenderIsOwner()
        {
            Assert(State.Owner.Value != null, "Contract not initialized.");
            Assert(State.Owner.Value == Context.Sender, "No permission.");
        }
    }
}