using System;
using Hidwizards.IOWrapper.Libraries.EmptyEventDictionary;
using HidWizards.IOWrapper.DataTransferObjects;

namespace Hidwizards.IOWrapper.Libraries.SubscriptionHandlers
{
    public class SubscriptionProcessor : EmptyEventDictionary<Guid, InputSubscriptionRequest, BindingDescriptor>
    {
        public delegate void CallbackHandler(InputSubscriptionRequest subreq, short value);
        public delegate void SequencedCallbackHandler(InputSubscriptionRequest subreq, ulong sequence, short value);

        private readonly CallbackHandler _callbackHandler;
        private readonly SequencedCallbackHandler _sequencedCallbackHandler;
        private ulong _sequence;

        public SubscriptionProcessor(BindingDescriptor emptyEventArgs, EventHandler<BindingDescriptor> emptyHandler, CallbackHandler callbackHandler)
            : base(emptyEventArgs, emptyHandler)
        {
            _callbackHandler = callbackHandler;
        }

        public SubscriptionProcessor(BindingDescriptor emptyEventArgs, EventHandler<BindingDescriptor> emptyHandler, SequencedCallbackHandler sequencedCallbackHandler)
         : base(emptyEventArgs, emptyHandler)
        {
            _sequencedCallbackHandler = sequencedCallbackHandler;
        }

        public bool FireCallbacks(BindingDescriptor bindingDescriptor, short value)
        {
            var block = false;
            var sequence = _sequence++;
            foreach (var inputSubscriptionRequest in Dictionary.Values)
            {
                if (_callbackHandler != null)
                {
                    _callbackHandler(inputSubscriptionRequest, value);
                }
                else if (_sequencedCallbackHandler != null)
                {
                    _sequencedCallbackHandler(inputSubscriptionRequest, sequence, value);
                }

                if (inputSubscriptionRequest.Block) block = true;
            }

            return block;
        }
    }
}