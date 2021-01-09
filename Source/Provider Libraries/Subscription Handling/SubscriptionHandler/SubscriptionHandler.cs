﻿using System;
using System.Collections.Generic;
using Hidwizards.IOWrapper.Libraries.EmptyEventDictionary;
using HidWizards.IOWrapper.DataTransferObjects;

namespace Hidwizards.IOWrapper.Libraries.SubscriptionHandlers
{
    public class SubscriptionHandler : ISubscriptionHandler
    {
        private readonly EmptyEventDictionary<BindingType,
            EmptyEventDictionary<int, EmptyEventDictionary<int, SubscriptionProcessor, BindingDescriptor>, BindingDescriptor>,
            DeviceDescriptor> _bindings;
        private readonly SubscriptionProcessor.CallbackHandler _callbackHandler;
        private readonly SubscriptionProcessor.SequencedCallbackHandler _sequencedCallbackHandler;

        public SubscriptionHandler(DeviceDescriptor deviceDescriptor, EventHandler<DeviceDescriptor> deviceEmptyHandler, SubscriptionProcessor.CallbackHandler callbackHandler)
        {
            _callbackHandler = callbackHandler;
            _bindings =
                new EmptyEventDictionary<BindingType,
                    EmptyEventDictionary<int, EmptyEventDictionary<int, SubscriptionProcessor, BindingDescriptor>,
                        BindingDescriptor>, DeviceDescriptor>(deviceDescriptor, deviceEmptyHandler);
        }

        public SubscriptionHandler(DeviceDescriptor deviceDescriptor, EventHandler<DeviceDescriptor> deviceEmptyHandler, SubscriptionProcessor.SequencedCallbackHandler sequencedCallbackHandler)
        {
            _sequencedCallbackHandler = sequencedCallbackHandler;
            _bindings =
                new EmptyEventDictionary<BindingType,
                    EmptyEventDictionary<int, EmptyEventDictionary<int, SubscriptionProcessor, BindingDescriptor>,
                        BindingDescriptor>, DeviceDescriptor>(deviceDescriptor, deviceEmptyHandler);
        }

        #region Subscriptions
        /// <summary>
        /// Add a subscription
        /// </summary>
        /// <param name="subReq">The Subscription Request object holding details of the subscription</param>
        public void Subscribe(InputSubscriptionRequest subReq)
        {
            var binding = _bindings.GetOrAdd(subReq.BindingDescriptor.Type,
                    new EmptyEventDictionary<int, EmptyEventDictionary<int, SubscriptionProcessor, BindingDescriptor>,
                        BindingDescriptor>(subReq.BindingDescriptor, BindingTypeEmptyHandler))
                .GetOrAdd(subReq.BindingDescriptor.Index,
                    new EmptyEventDictionary<int, SubscriptionProcessor, BindingDescriptor>(subReq.BindingDescriptor,
                        IndexEmptyHandler)); 

            if (_callbackHandler != null)
            {
                binding.GetOrAdd(subReq.BindingDescriptor.SubIndex,
                    new SubscriptionProcessor(subReq.BindingDescriptor, SubIndexEmptyHandler, _callbackHandler))
                    .TryAdd(subReq.SubscriptionDescriptor.SubscriberGuid, subReq);
            }
            else if (_sequencedCallbackHandler != null)
            {
                binding.GetOrAdd(subReq.BindingDescriptor.SubIndex,
                    new SubscriptionProcessor(subReq.BindingDescriptor, SubIndexEmptyHandler, _sequencedCallbackHandler))
                    .TryAdd(subReq.SubscriptionDescriptor.SubscriberGuid, subReq);
            }
        }

        /// <summary>
        /// Remove a subscription
        /// </summary>
        /// <param name="subReq">The Subscription Request object holding details of the subscription</param>
        public void Unsubscribe(InputSubscriptionRequest subReq)
        {
            if (ContainsKey(subReq.BindingDescriptor.Type, subReq.BindingDescriptor.Index, subReq.BindingDescriptor.SubIndex))
            {
                _bindings[subReq.BindingDescriptor.Type][subReq.BindingDescriptor.Index][subReq.BindingDescriptor.SubIndex].TryRemove(subReq.SubscriptionDescriptor.SubscriberGuid, out _);
            }
        }

        /// <summary>
        /// Fires all subscription callbacks for a given Type / Index / SubIndex
        /// </summary>
        /// <param name="bindingDescriptor">A Binding describing the binding</param>
        /// <param name="value">The new value for the input</param>
        public bool FireCallbacks(BindingDescriptor bindingDescriptor, short value)
        {
            if (ContainsKey(bindingDescriptor.Type, bindingDescriptor.Index, bindingDescriptor.SubIndex))
            {
                return _bindings[bindingDescriptor.Type][bindingDescriptor.Index][bindingDescriptor.SubIndex].FireCallbacks(bindingDescriptor, value);
            }

            return false;
        }
        #endregion

        #region Dictionary counting and querying

        #region ContainsKey

        // Are there any Axis / Button / POV subscriptions?
        public bool ContainsKey(BindingType bindingType)
        {
            return _bindings.ContainsKey(bindingType);
        }

        // Which Axes / Buttons have subscriptions?
        public bool ContainsKey(BindingType bindingType, int index)
        {
            return _bindings.ContainsKey(bindingType) && _bindings[bindingType].ContainsKey(index);
        }

        public bool ContainsKey(BindingType bindingType, int index, int subIndex)
        {
            return ContainsKey(bindingType, index) && _bindings[bindingType][index].ContainsKey(subIndex);
        }
        #endregion

        #region GetKeys

        // Which BindingTypes have subscriptions?
        public IEnumerable<BindingType> GetKeys()
        {
            return _bindings.GetKeys();
        }

        // Which Indexes have subscriptions?
        public IEnumerable<int> GetKeys(BindingType bindingType)
        {
            return _bindings[bindingType].GetKeys();
        }

        // Which SubIndexes have subscriptions?
        public IEnumerable<int> GetKeys(BindingType bindingType, int index)
        {
            return _bindings[bindingType][index].GetKeys();
        }

        #endregion

        #region Count

        public int Count()
        {
            return _bindings.Count();
        }

        public int Count(BindingType bindingType)
        {
            return ContainsKey(bindingType) ? _bindings[bindingType].Count() : 0;
        }

        public int Count(BindingType bindingType, int index)
        {
            return ContainsKey(bindingType, index) ? _bindings[bindingType][index].Count() : 0;
        }

        #endregion

        #endregion

        /// <summary>
        /// Gets called when a given BindingType (Axes, Buttons or POVs) no longer has any subscriptions
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="bindingDescriptor">A Binding describing the binding</param>
        private void BindingTypeEmptyHandler(object sender, BindingDescriptor bindingDescriptor)
        {
            _bindings.TryRemove(bindingDescriptor.Type, out _);
        }

        /// <summary>
        /// Gets called when a given Index (A single Axis, Button or POV) no longer has any subscriptions
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="bindingDescriptor">A Binding describing the binding</param>
        private void IndexEmptyHandler(object sender, BindingDescriptor bindingDescriptor)
        {
            _bindings[bindingDescriptor.Type].TryRemove(bindingDescriptor.Index, out _);
        }

        /// <summary>
        /// Gets called when a given SubIndex (eg POV direction) no longer has any subscriptions
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="bindingDescriptor">A Binding describing the binding</param>
        private void SubIndexEmptyHandler(object sender, BindingDescriptor bindingDescriptor)
        {
            _bindings[bindingDescriptor.Type][bindingDescriptor.Index].TryRemove(bindingDescriptor.SubIndex, out _);
        }
    }
}
