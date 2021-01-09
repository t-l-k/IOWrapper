﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HidWizards.IOWrapper.DataTransferObjects
{
    /// <summary>
    /// SubscriptionRequests allow the front end to add or remove Bindings
    /// </summary>
    public class SubscriptionRequest
    {
        /// <summary>
        /// Identifies the Subscriber
        /// </summary>
        public SubscriptionDescriptor SubscriptionDescriptor { get; set; }

        /// <summary>
        /// Identifies the Provider that this subscription is for
        /// </summary>
        public ProviderDescriptor ProviderDescriptor { get; set; }

        /// <summary>
        /// Identifies which (Provider-specific) Device that this subscription is for
        /// </summary>
        public DeviceDescriptor DeviceDescriptor { get; set; }
    }

    /// <summary>
    /// Contains all the required information for :
    ///     The IOController to route the request to the appropriate Provider
    ///     The Provider to subscribe to the appropriate input
    ///     The Provider to notify the subscriber of activity
    /// </summary>
    public class InputSubscriptionRequest : SubscriptionRequest
    {
        /// <summary>
        /// Identifies the (Provider+Device-specific) Input that this subscription is for
        /// </summary>
        public BindingDescriptor BindingDescriptor { get; set; }

        /// <summary>
        /// Whether or not to request that this input it Blocked (Hidden from system)
        /// Not all providers will support this
        /// </summary>
        public bool Block { get; set; }

        /// <summary>
        /// Callback to be fired when this Input changes state
        /// </summary>
        //public dynamic Callback { get; set; }
        // Disabled, as enabling this breaks a bunch of stuff in UCR
        public Action<short> Callback { get; set; }

        /// <summary>
        /// Callback to be fired when this Input changes state in volatile threaded contexts
        /// </summary>
        public Action<ulong, short> SequencedCallback { get; set; }

        public InputSubscriptionRequest Clone()
        {
            return (InputSubscriptionRequest)MemberwiseClone();
        }
    }

    /// <summary>
    /// Contains all the information for:
    ///     The IOController to route the request to the appropriate Provider
    ///
    /// Output Subscriptions are typically used to eg create virtual devices...
    /// ... so that output can be sent to them
    /// </summary>
    public class OutputSubscriptionRequest : SubscriptionRequest
    {
        public OutputSubscriptionRequest Clone()
        {
            return (OutputSubscriptionRequest)MemberwiseClone();
        }
    }

}
