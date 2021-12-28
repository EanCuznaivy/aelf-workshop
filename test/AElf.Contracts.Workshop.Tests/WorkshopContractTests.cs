using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Association;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Profit;
using AElf.Contracts.Vote;
using AElf.ContractTestBase.ContractTestKit;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Workshop
{
    public class WorkshopContractTests : WorkshopContractTestBase
    {
        [Fact]
        public async Task WorkshopContractTest()
        {
            var keyPair = SampleAccount.Accounts.First().KeyPair;
            var stub = GetWorkshopContractStub(keyPair);
            var anotherKeyPair = SampleAccount.Accounts.Skip(1).First().KeyPair;
            var anotherStub = GetWorkshopContractStub(anotherKeyPair);

            {
                var executionResult = await stub.Hello.SendAsync(new HelloInput
                {
                    Name = "aelf workshop",
                });

                executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            await stub.StartWorkshop.SendAsync(new StartWorkshopInput
            {
                Title = "Usage of aelf-cli"
            });

            var workshop = await stub.GetCurrentWorkshop.CallAsync(new Empty());
            workshop.Id.ShouldBe(1);
            workshop.Title.ShouldBe("Usage of aelf-cli");
            workshop.StartTime.ShouldNotBeNull();
            workshop.StartBalance.ShouldBePositive();

            {
                var executionResult = await anotherStub.EndWorkshop.SendWithExceptionAsync(new EndWorkshopInput());
                executionResult.TransactionResult.Error.ShouldContain("No permission.");
            }
        }

        [Fact]
        internal async Task<TokenContractContainer.TokenContractStub> MultiTokenContractTest()
        {
            var issuerAccount = SampleAccount.Accounts.First();
            var userAccount = SampleAccount.Accounts.Skip(1).First();
            var spenderAccount = SampleAccount.Accounts.Skip(2).First();
            var issuerStub = GetTokenContractStub(issuerAccount.KeyPair);
            var userStub = GetTokenContractStub(userAccount.KeyPair);
            var spenderStub = GetTokenContractStub(spenderAccount.KeyPair);

            // Create tokens.
            await issuerStub.Create.SendAsync(new CreateInput
            {
                Symbol = "EAN",
                Decimals = 8,
                Issuer = issuerAccount.Address,
                IsBurnable = true,
                TokenName = "Ean token",
                TotalSupply = 100_000_000,
                LockWhiteList = {VoteContractAddress}
            });

            var tokenInfo = await issuerStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
            {
                Symbol = "EAN"
            });
            tokenInfo.Issuer.ShouldBe(issuerAccount.Address);

            // Issue
            await issuerStub.Issue.SendAsync(new IssueInput
            {
                To = userAccount.Address,
                Amount = 100,
                Symbol = "EAN"
            });

            {
                var userBalance = await issuerStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = userAccount.Address,
                    Symbol = "EAN"
                });
                userBalance.Balance.ShouldBe(100);
            }

            // Transfer
            await userStub.Transfer.SendAsync(new TransferInput
            {
                To = issuerAccount.Address,
                Symbol = "EAN",
                Amount = 1
            });

            {
                var userBalance = await issuerStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = userAccount.Address,
                    Symbol = "EAN"
                });
                userBalance.Balance.ShouldBe(99);
            }

            // Approve
            await userStub.Approve.SendAsync(new ApproveInput
            {
                Spender = spenderAccount.Address,
                Symbol = "EAN",
                Amount = 100
            });
            {
                var allowance = await issuerStub.GetAllowance.CallAsync(new GetAllowanceInput
                {
                    Owner = userAccount.Address,
                    Spender = spenderAccount.Address,
                    Symbol = "EAN"
                });
                allowance.Allowance.ShouldBe(100);
            }

            // TransferFrom
            await spenderStub.TransferFrom.SendAsync(new TransferFromInput
            {
                From = userAccount.Address,
                To = spenderAccount.Address,
                Amount = 10,
                Symbol = "EAN"
            });
            {
                var allowance = await issuerStub.GetAllowance.CallAsync(new GetAllowanceInput
                {
                    Owner = userAccount.Address,
                    Spender = spenderAccount.Address,
                    Symbol = "EAN"
                });
                allowance.Allowance.ShouldBe(90);
            }
            {
                var userBalance = await issuerStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = userAccount.Address,
                    Symbol = "EAN"
                });
                userBalance.Balance.ShouldBe(89);
            }

            return userStub;
        }

        [Fact]
        public async Task VoteContractTest()
        {
            var tokenStub = await MultiTokenContractTest();

            var creatorAccount = SampleAccount.Accounts.First();
            var voterAccounts = SampleAccount.Accounts.Skip(5).Take(5).ToList();
            var creatorStub = GetVoteContractStub(creatorAccount.KeyPair);
            var voterStubs = voterAccounts.Select(a => GetVoteContractStub(a.KeyPair));

            var executionResult = await creatorStub.Register.SendAsync(new VotingRegisterInput
            {
                AcceptedCurrency = "EAN",
                IsLockToken = true,
                Options =
                {
                    "TOP Of OASIS",
                    "Peak Hunters",
                    "Back To The Future",
                    "Ready Hacker One"
                },
                StartTimestamp = TimestampHelper.GetUtcNow(),
                EndTimestamp = TimestampHelper.GetUtcNow().AddDays(1)
            });
            var votingItemId = VotingItemRegistered.Parser.ParseFrom(executionResult.TransactionResult.Logs
                    .Single(l => l.Name == nameof(VotingItemRegistered)).NonIndexed)
                .VotingItemId;

            var votingItem =
                await creatorStub.GetVotingItem.CallAsync(new GetVotingItemInput {VotingItemId = votingItemId});
            votingItem.Options.Count.ShouldBe(4);

            // Transfer EAN tokens to each voter.
            foreach (var voterAccount in voterAccounts)
            {
                await tokenStub.Transfer.SendAsync(new TransferInput
                {
                    Symbol = "EAN",
                    Amount = 1,
                    To = voterAccount.Address
                });
            }

            using var optionVector = new List<string>
            {
                "TOP Of OASIS",
                "Peak Hunters",
                "Back To The Future",
                "Ready Hacker One",
                "TOP Of OASIS",
            }.GetEnumerator();
            foreach (var voterStub in voterStubs)
            {
                optionVector.MoveNext();
                var option = optionVector.Current;
                await voterStub.Vote.SendAsync(new VoteInput
                {
                    Amount = 1,
                    Option = option,
                    VotingItemId = votingItemId,
                });
            }

            await creatorStub.TakeSnapshot.SendAsync(new TakeSnapshotInput
            {
                SnapshotNumber = 1,
                VotingItemId = votingItemId
            });

            var votingResult = await creatorStub.GetVotingResult.CallAsync(new GetVotingResultInput
            {
                SnapshotNumber = 1,
                VotingItemId = votingItemId
            });
            votingResult.Results["TOP Of OASIS"].ShouldBe(2);
        }

        [Fact]
        public async Task ProfitContractTest()
        {
            var creatorAccount = SampleAccount.Accounts.First();
            var beneficiaryAccounts = SampleAccount.Accounts.Skip(10).Take(5).ToList();
            var creatorStub = GetProfitContractStub(creatorAccount.KeyPair);
            var beneficiaryStubs = beneficiaryAccounts.Select(a => GetProfitContractStub(a.KeyPair));

            var executionResult = await creatorStub.CreateScheme.SendAsync(new CreateSchemeInput
            {
                Manager = creatorAccount.Address,
                IsReleaseAllBalanceEveryTimeByDefault = true,
                CanRemoveBeneficiaryDirectly = true
            });
            var schemeId = executionResult.Output;

            var scheme = await creatorStub.GetScheme.CallAsync(schemeId);
            scheme.Manager.ShouldBe(creatorAccount.Address);

            // Add beneficiaries.
            await creatorStub.AddBeneficiaries.SendAsync(new AddBeneficiariesInput
            {
                SchemeId = schemeId,
                BeneficiaryShares =
                {
                    beneficiaryAccounts.Select(a => new BeneficiaryShare
                    {
                        Beneficiary = a.Address,
                        Shares = 1
                    })
                }
            });

            // Contribute.
            await creatorStub.ContributeProfits.SendAsync(new ContributeProfitsInput
            {
                Symbol = "ELF",
                SchemeId = schemeId,
                Amount = 1000,
                Period = 1
            });

            // Distribute.
            await creatorStub.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                Period = 1,
                AmountsMap =
                {
                    {"ELF", 0}
                }
            });

            using var beneficiaryAccountsVector = beneficiaryAccounts.GetEnumerator();
            foreach (var beneficiaryStub in beneficiaryStubs)
            {
                beneficiaryAccountsVector.MoveNext();
                await beneficiaryStub.ClaimProfits.SendAsync(new ClaimProfitsInput
                {
                    Beneficiary = beneficiaryAccountsVector.Current.Address,
                    SchemeId = schemeId
                });
                // Check balance.
                var tokenStub = GetTokenContractStub(creatorAccount.KeyPair);
                var balance = await tokenStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Symbol = "ELF",
                    Owner = beneficiaryAccountsVector.Current.Address
                });
                balance.Balance.ShouldBe(200);
            }
        }

        [Fact]
        public async Task AssociationContractTest()
        {
            var creatorAccount = SampleAccount.Accounts.First();
            var memberAccounts = SampleAccount.Accounts.Skip(15).Take(5).ToList();
            var creatorStub = GetAssociationContractStub(creatorAccount.KeyPair);
            var memberStubs = memberAccounts.Select(a => GetAssociationContractStub(a.KeyPair)).ToList();

            var executionResult = await creatorStub.CreateOrganization.SendAsync(new CreateOrganizationInput
            {
                OrganizationMemberList = new OrganizationMemberList
                {
                    OrganizationMembers =
                    {
                        memberAccounts.Select(a => a.Address),
                        creatorAccount.Address
                    }
                },
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MinimalApprovalThreshold = 3,
                    MinimalVoteThreshold = 4,
                    MaximalRejectionThreshold = 2,
                    MaximalAbstentionThreshold = 2
                },
                ProposerWhiteList = new ProposerWhiteList
                {
                    Proposers =
                    {
                        memberAccounts.Select(a => a.Address),
                        creatorAccount.Address
                    }
                }
            });
            var organizationAddress = executionResult.Output;

            var randomMemberStub = memberStubs.First();
            var proposalId = (await randomMemberStub.CreateProposal.SendAsync(new CreateProposalInput
            {
                OrganizationAddress = organizationAddress,
                ContractMethodName = "ResetOwner",
                ToAddress = DAppContractAddress,
                Params = memberAccounts.First().Address.ToByteString(),
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1)
            })).Output;

            // Approves
            foreach (var memberStub in memberStubs)
            {
                await memberStub.Approve.SendAsync(proposalId);
            }

            await randomMemberStub.Release.SendAsync(proposalId);

            var workshopContractStub = GetWorkshopContractStub(creatorAccount.KeyPair);
            var owner = await workshopContractStub.GetOwner.CallAsync(new Empty());
            owner.ShouldBe(memberAccounts.First().Address);
        }
    }
}