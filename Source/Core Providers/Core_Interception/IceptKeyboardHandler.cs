﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core_Interception.Lib;
using Hidwizards.IOWrapper.Libraries.PollingDeviceHandler.Devices;
using Hidwizards.IOWrapper.Libraries.PollingDeviceHandler.Updates;
using Hidwizards.IOWrapper.Libraries.SubscriptionHandlerNs;
using HidWizards.IOWrapper.DataTransferObjects;

namespace Core_Interception
{
    public class IceptKeyboardHandler : PolledDeviceHandler<ManagedWrapper.Stroke, (BindingType, int)>
    {
        private readonly IceptDeviceLibrary _deviceLibrary;

        public IceptKeyboardHandler(DeviceDescriptor deviceDescriptor, IceptDeviceLibrary deviceLibrary) : base(deviceDescriptor)
        {
            _deviceLibrary = deviceLibrary;
        }

        protected override IDeviceUpdateHandler<ManagedWrapper.Stroke> CreateUpdateHandler(DeviceDescriptor deviceDescriptor, SubscriptionHandler subscriptionHandler,
            EventHandler<BindModeUpdate> bindModeHandler)
        {
            return new IceptKeyboardUpdateHandler(deviceDescriptor, SubHandler, bindModeHandler, _deviceLibrary);
        }

        public override void Poll(ManagedWrapper.Stroke update)
        {
            DeviceUpdateHandler.ProcessUpdate(update);
        }
    }
}
