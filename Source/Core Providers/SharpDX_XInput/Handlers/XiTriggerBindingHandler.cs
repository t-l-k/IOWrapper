﻿using HidWizards.IOWrapper.ProviderInterface;
using HidWizards.IOWrapper.ProviderInterface.Handlers;
using HidWizards.IOWrapper.DataTransferObjects;

namespace SharpDX_XInput.Handlers
{
    public class XiTriggerBindingHandler : BindingHandler
    {
        public XiTriggerBindingHandler(InputSubscriptionRequest subReq) : base(subReq) { }

        public override void Poll(int pollValue)
        {
            // Normalization of Axes to standard scale occurs here
            BindingDictionary[0].State =
                (pollValue * 257) - 32768;
        }
    }
}