﻿using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Data.WorkflowAPI;
using PX.Objects.AR;
using PX.Objects.Common;
using static PX.Data.WorkflowAPI.BoundedTo<PhoneRepairShop.RSSVWorkOrderEntry,
  PhoneRepairShop.RSSVWorkOrder>;

namespace PhoneRepairShop.Workflows
{
    public class RSSVWorkOrderWorkflow : PX.Data.PXGraphExtension<RSSVWorkOrderEntry>
    {
        #region Constants
        public static class States
        {
            public const string OnHold = WorkOrderStatusConstants.OnHold;
            public const string ReadyForAssignment = WorkOrderStatusConstants.ReadyForAssignment;
            public const string PendingPayment = WorkOrderStatusConstants.PendingPayment;
            public const string Assigned = WorkOrderStatusConstants.Assigned;
            public const string Completed = WorkOrderStatusConstants.Completed;
            public const string Paid = WorkOrderStatusConstants.Paid;

            public class onHold : PX.Data.BQL.BqlString.Constant<onHold>
            {
                public onHold() : base(OnHold) { }
            }

            public class readyForAssignment :
              PX.Data.BQL.BqlString.Constant<readyForAssignment>
            {
                public readyForAssignment() : base(ReadyForAssignment) { }
            }

            public class pendingPayment :
              PX.Data.BQL.BqlString.Constant<pendingPayment>
            {
                public pendingPayment() : base(PendingPayment) { }
            }

            public class assigned : PX.Data.BQL.BqlString.Constant<assigned>
            {
                public assigned() : base(Assigned) { }
            }

            public class completed : PX.Data.BQL.BqlString.Constant<completed>
            {
                public completed() : base(Completed) { }
            }

            public class paid : PX.Data.BQL.BqlString.Constant<paid>
            {
                public paid() : base(Paid) { }
            }
        }
        #endregion

        #region Conditions
        public class Conditions : Condition.Pack
        {
            public Condition RequiresPrepayment => GetOrCreate(b => b.FromBql<
               Where<Selector<RSSVWorkOrder.serviceID, RSSVRepairService.prepayment>,
               Equal<True>>>());
            public Condition DoesNotRequirePrepayment => GetOrCreate(b => b.FromBql<
               Where<Selector<RSSVWorkOrder.serviceID, RSSVRepairService.prepayment>,
               Equal<False>>>());
            public Condition DisableCreateInvoice => GetOrCreate(b => b.FromBql<
               Where<RSSVWorkOrder.invoiceNbr.IsNotNull>>());
        }
        #endregion

        public override void Configure(PXScreenConfiguration config)
        {
            var context = config.GetScreenConfigurationContext<RSSVWorkOrderEntry, RSSVWorkOrder>();

            var formAssign = context.Forms.Create("FormAssign", form =>
                form.Prompt("Select Assignee").WithFields(fields =>
                {
                    fields.Add("Assignee", field => field
                      .WithSchemaOf<RSSVWorkOrder.assignee>()
                      .IsRequired()
                      .Prompt("Assignee"));
                }));


            var conditions = context.Conditions.GetPack<Conditions>();

            #region Categories
            var commonCategories = CommonActionCategories.Get(context);
            var processingCategory = commonCategories.Processing;
            #endregion

            context.AddScreenConfigurationFor(screen => screen
                .StateIdentifierIs<RSSVWorkOrder.status>()
                .AddDefaultFlow(flow => flow
                    .WithFlowStates(fss =>
                    {
                        fss.Add<States.onHold>(flowState =>
                        {
                            return flowState
                                .IsInitial()
                                .WithActions(actions =>
                                {
                                    actions.Add(g => g.ReleaseFromHold, a => a
                                        .IsDuplicatedInToolbar()
                                        .WithConnotation(ActionConnotation.Success));
                                });
                        });

                        fss.Add<States.readyForAssignment>(flowState =>
                        {
                            return flowState
                                .WithFieldStates(states =>
                                {
                                    states.AddField<RSSVWorkOrder.customerID>(state => state.IsDisabled());
                                    states.AddField<RSSVWorkOrder.serviceID>(state => state.IsDisabled());
                                    states.AddField<RSSVWorkOrder.deviceID>(state => state.IsDisabled());
                                })
                                .WithActions(actions =>
                                {
                                    actions.Add(g => g.PutOnHold, a => a.IsDuplicatedInToolbar());
                                    actions.Add(g => g.Assign, a => a
                                          .IsDuplicatedInToolbar()
                                          .WithConnotation(ActionConnotation.Success));
                                });
                        });

                        fss.Add<States.pendingPayment>(flowState =>
                        {
                            return flowState
                                .WithFieldStates(states =>
                                {
                                    states.AddField<RSSVWorkOrder.customerID>(state => state.IsDisabled());
                                    states.AddField<RSSVWorkOrder.serviceID>(state => state.IsDisabled());
                                    states.AddField<RSSVWorkOrder.deviceID>(state => state.IsDisabled());
                                })
                                .WithActions(actions =>
                                {
                                    actions.Add(g => g.PutOnHold, a => a.IsDuplicatedInToolbar());
                                    actions.Add(g => g.CreateInvoiceAction, a => a
                                        .IsDuplicatedInToolbar()
                                        .WithConnotation(ActionConnotation.Success));
                                })
                                .WithEventHandlers(handlers =>
                                {
                                    handlers.Add(g => g.OnInvoiceGotPrepaid);
                                }); ;
                        });

                        fss.Add<States.assigned>(flowState =>
                        {
                            return flowState
                                .WithFieldStates(states =>
                                {
                                    states.AddField<RSSVWorkOrder.customerID>(state => state.IsDisabled());
                                    states.AddField<RSSVWorkOrder.serviceID>(state => state.IsDisabled());
                                    states.AddField<RSSVWorkOrder.deviceID>(state => state.IsDisabled());
                                })
                                .WithActions(actions =>
                                {
                                    actions.Add(g => g.Complete, a => a
                                      .IsDuplicatedInToolbar()
                                      .WithConnotation(ActionConnotation.Success));
                                });
                        });

                        fss.Add<States.completed>(flowState =>
                        {
                            return flowState
                              .WithFieldStates(states =>
                              {
                                  states.AddField<RSSVWorkOrder.customerID>(state => state.IsDisabled());
                                  states.AddField<RSSVWorkOrder.serviceID>(state => state.IsDisabled());
                                  states.AddField<RSSVWorkOrder.deviceID>(state => state.IsDisabled());
                              })
                              .WithActions(actions =>
                              {
                                  actions.Add(g => g.CreateInvoiceAction, a => a
                                    .IsDuplicatedInToolbar()
                                    .WithConnotation(ActionConnotation.Success));
                              })
                              .WithEventHandlers(handlers =>
                              {
                                  handlers.Add(g => g.OnCloseDocument);
                              }); ;
                        });

                        fss.Add<States.paid>(flowState =>
                        {
                            return flowState
                                .WithFieldStates(states =>
                                {
                                    states.AddField<RSSVWorkOrder.customerID>(state => state.IsDisabled());
                                    states.AddField<RSSVWorkOrder.serviceID>(state => state.IsDisabled());
                                    states.AddField<RSSVWorkOrder.deviceID>(state => state.IsDisabled());
                                });
                        });
                    })
                    .WithTransitions(transitions =>
                    {
                        transitions.AddGroupFrom<States.onHold>(ts =>
                        {
                            ts.Add(t => t.To<States.readyForAssignment>()
                                .IsTriggeredOn(g => g.ReleaseFromHold)
                                .When(conditions.DoesNotRequirePrepayment)
                                );
                            ts.Add(t => t.To<States.pendingPayment>()
                                .IsTriggeredOn(g => g.ReleaseFromHold)
                                .When(conditions.RequiresPrepayment));
                        });
                        transitions.AddGroupFrom<States.readyForAssignment>(ts =>
                        {
                            ts.Add(t => t.To<States.onHold>().IsTriggeredOn(g => g.PutOnHold));
                            ts.Add(t => t.To<States.assigned>().IsTriggeredOn(g => g.Assign));
                        });
                        transitions.AddGroupFrom<States.pendingPayment>(ts =>
                        {
                            ts.Add(t => t.To<States.onHold>().IsTriggeredOn(g => g.PutOnHold));
                            ts.Add(t => t.To<States.readyForAssignment>()
                                .IsTriggeredOn(g => g.OnInvoiceGotPrepaid));
                        });
                        transitions.AddGroupFrom<States.assigned>(ts =>
                        {
                            ts.Add(t => t.To<States.completed>().IsTriggeredOn(g => g.Complete));
                        });
                        transitions.AddGroupFrom<States.completed>(ts =>
                        {
                            ts.Add(t => t.To<States.paid>().IsTriggeredOn(g => g.OnCloseDocument));
                        });
                    }))
                .WithCategories(categories =>
                {
                    categories.Add(processingCategory);
                })
                .WithActions(actions =>
                {
                    actions.Add(g => g.ReleaseFromHold, c => c
                            .WithCategory(processingCategory));
                    actions.Add(g => g.PutOnHold, c => c
                            .WithCategory(processingCategory));
                    actions.Add(g => g.Assign, c => c
                        .WithCategory(processingCategory)
                        .WithForm(formAssign)
                        .WithFieldAssignments(fields => {
                            fields.Add<RSSVWorkOrder.assignee>(f => 
                                f.SetFromFormField(formAssign, "Assignee"));
                        }));
                    actions.Add(g => g.Complete, c => c
                      .WithCategory(processingCategory)
                      .WithFieldAssignments(fas => fas.Add<RSSVWorkOrder.dateCompleted>(f =>
                        f.SetFromToday())));
                    actions.Add(g => g.CreateInvoiceAction, c => c
                      .WithCategory(processingCategory)
                      .IsDisabledWhen(conditions.DisableCreateInvoice));
                })
                .WithForms(forms => forms.Add(formAssign))
                .WithHandlers(handlers =>
                {
                    handlers.Add(handler => handler
                      .WithTargetOf<ARInvoice>()
                      .OfEntityEvent<ARInvoice.Events>(e => e.CloseDocument)
                      .Is(g => g.OnCloseDocument)
                      .UsesPrimaryEntityGetter<
                        SelectFrom<RSSVWorkOrder>.
                        Where<RSSVWorkOrder.invoiceNbr.IsEqual<ARRegister.refNbr.FromCurrent>>>());
                    handlers.Add(handler => handler
                        .WithTargetOf<ARRegister>()
                        .OfEntityEvent<RSSVWorkOrder.MyEvents>(e => e.InvoiceGotPrepaid)
                        .Is(g => g.OnInvoiceGotPrepaid)
                        .UsesPrimaryEntityGetter<
                            SelectFrom<RSSVWorkOrder>.
                            Where<RSSVWorkOrder.invoiceNbr.IsEqual<ARRegister.refNbr.FromCurrent>>>());
                }));
        }
    }
}