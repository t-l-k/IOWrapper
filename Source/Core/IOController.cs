﻿using System.Collections.Generic;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using HidWizards.IOWrapper.Core.Exceptions;
using HidWizards.IOWrapper.DataTransferObjects;
using HidWizards.IOWrapper.ProviderInterface;
using HidWizards.IOWrapper.ProviderInterface.Interfaces;

namespace HidWizards.IOWrapper.Core
{
    public enum InputTypes { Button, Axis }

    public class IOController : IDisposable
    {
        bool disposed;
        private Dictionary<string, IProvider> _providers;
        private readonly Dictionary<Guid, InputSubscriptionRequest> ActiveInputSubscriptions = new Dictionary<Guid, InputSubscriptionRequest>();
        private readonly Dictionary<Guid, OutputSubscriptionRequest> ActiveOutputSubscriptions = new Dictionary<Guid, OutputSubscriptionRequest>();

        public IOController()
        {
            var executingFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var providerFolder = Path.Combine(executingFolder ?? throw new InvalidOperationException(), "Providers");
            var loader = new GenericMEFPluginLoader<IProvider>($"{providerFolder}");
            _providers = new Dictionary<string, IProvider>();
            var providers = loader.Plugins;
            Log("Initializing...");
            foreach (var lazyProvider in providers)
            {
                try
                {
                    var provider = lazyProvider.Value;
                    _providers[provider.ProviderName] = provider;
                    Log("Initialized Provider {0}", provider.ProviderName);
                }
                catch(CompositionException ex)
                {
                    // Plugin failed to load
                    Log(ex.RootCauses.First().Message);
                }
                
            }
            Log("Initialization complete");
        }

        ~IOController()
        {
            Dispose(true);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;
            if (disposing)
            {
                foreach (var provider in _providers.Values)
                {
                    provider.Dispose();
                }
                _providers = null;
            }
            disposed = true;
            Log("Disposed");
        }

        private static void Log(string formatStr, params object[] arguments)
        {
            Debug.WriteLine("IOWrapper| IOController| " + formatStr, arguments);
        }

        public SortedDictionary<string, ProviderReport> GetInputList()
        {
            var list = new SortedDictionary<string, ProviderReport>();
            foreach (var provider in _providers.Values)
            {
                if (!(provider is IInputProvider prov)) continue;
                var report = prov.GetInputList();
                if (report != null)
                {
                    list.Add(provider.ProviderName, report);
                }
            }
            return list;
        }

        public SortedDictionary<string, ProviderReport> GetOutputList()
        {
            var list = new SortedDictionary<string, ProviderReport>();
            foreach (var provider in _providers.Values)
            {
                if (!(provider is IOutputProvider prov)) continue;
                var report = prov.GetOutputList();
                if (report != null)
                {
                    list.Add(provider.ProviderName, report);
                }
            }
            return list;
        }

        public DeviceReport GetInputDeviceReport(ProviderDescriptor providerDescriptor, DeviceDescriptor deviceDescriptor)
        {
            return GetProvider<IInputProvider>(providerDescriptor.ProviderName)
                .GetInputDeviceReport(deviceDescriptor);
        }

        public DeviceReport GetOutputDeviceReport(ProviderDescriptor providerDescriptor, DeviceDescriptor deviceDescriptor)
        {
            return GetProvider<IOutputProvider>(providerDescriptor.ProviderName)
                .GetOutputDeviceReport(deviceDescriptor);
        }

        public bool SubscribeInput(InputSubscriptionRequest _subReq)
        {
            SubscribeInputWithExceptions(_subReq);
            return true;
        }

        public void SubscribeInputWithExceptions(InputSubscriptionRequest _subReq)
        {
            // Clone subreq before passing to provider, so if it gets altered outside, it does not affect the copy
            var subReq = _subReq.Clone();
            LogInputSubReq("SubscribeInput", subReq);
            var provider = GetProvider<IInputProvider>(subReq.ProviderDescriptor.ProviderName);
            try
            {
                if (ActiveInputSubscriptions.ContainsKey(subReq.SubscriptionDescriptor.SubscriberGuid))
                {
                    // If this Subscriber has an existing subscription...
                    // ... then remove the old subscription first.
                    Log("Existing subscription found, removing...");
                    var oldSub = ActiveInputSubscriptions[subReq.SubscriptionDescriptor.SubscriberGuid];
                    UnsubscribeInput(oldSub);
                }
                provider.SubscribeInput(subReq);
                ActiveInputSubscriptions.Add(subReq.SubscriptionDescriptor.SubscriberGuid, subReq);
            }
            catch (Exception ex)
            {
                throw new IOControllerExceptions.SubscribeInputFailedException(ex, provider, subReq); 
            }
        }

        public bool UnsubscribeInput(InputSubscriptionRequest _subReq)
        {
            UnsubscribeInputWithExceptions(_subReq);
            return true;
        }

        public void UnsubscribeInputWithExceptions(InputSubscriptionRequest _subReq)
        {
            var subReq = _subReq.Clone();
            LogInputSubReq("UnsubscribeInput", subReq);
            if (ActiveInputSubscriptions.ContainsKey(subReq.SubscriptionDescriptor.SubscriberGuid))
            {
                var provider = GetProvider<IInputProvider>(subReq.ProviderDescriptor.ProviderName);
                try
                {
                    provider.UnsubscribeInput(ActiveInputSubscriptions[subReq.SubscriptionDescriptor.SubscriberGuid]);
                    ActiveInputSubscriptions.Remove(subReq.SubscriptionDescriptor.SubscriberGuid);
                }
                catch (Exception ex)
                {
                    throw new IOControllerExceptions.UnsubscribeInputFailedException(ex, provider, subReq);
                }
            }
            else
            {
                throw new IOControllerExceptions.SubscriptionNotFoundException(subReq);
            }
        }

        public void SetDetectionMode(DetectionMode detectionMode, ProviderDescriptor providerDescriptor, DeviceDescriptor deviceDescriptor, Action<ProviderDescriptor, DeviceDescriptor, BindingReport, short> callback = null)
        {
            var provider = GetProvider<IBindModeProvider>(providerDescriptor.ProviderName);
            provider.SetDetectionMode(detectionMode, deviceDescriptor, callback);
        }

        private void LogInputSubReq(string title, InputSubscriptionRequest subReq)
        {
            Log("{0}: Provider {1}, Device {2}, Type {3}, Index {4}, SubIndex {5}, Guid {6}",
                title,
                subReq.ProviderDescriptor.ProviderName,
                subReq.DeviceDescriptor.DeviceHandle,
                subReq.BindingDescriptor.Type.ToString(),
                subReq.BindingDescriptor.Index,
                subReq.BindingDescriptor.SubIndex,
                subReq.SubscriptionDescriptor.SubscriberGuid);
        }

        public bool SubscribeOutput(OutputSubscriptionRequest _subReq)
        {
            SubscribeOutputWithExceptions(_subReq);
            return true;
        }

        public void SubscribeOutputWithExceptions(OutputSubscriptionRequest _subReq)
        {
            var subReq = _subReq.Clone();
            LogOutputSubReq("SubscribeOutput", subReq);
            if (ActiveOutputSubscriptions.ContainsKey(subReq.SubscriptionDescriptor.SubscriberGuid))
            {
                // If this Subscriber has an existing subscription...
                // ... then remove the old subscription first.
                // unsub output here
                UnsubscribeOutput(ActiveOutputSubscriptions[subReq.SubscriptionDescriptor.SubscriberGuid]);
            }
            var provider = GetProvider<IOutputProvider>(subReq.ProviderDescriptor.ProviderName);
            try
            {
                provider.SubscribeOutputDevice(subReq);
                ActiveOutputSubscriptions.Add(subReq.SubscriptionDescriptor.SubscriberGuid, subReq);
            }
            catch (Exception ex)
            {
                throw new IOControllerExceptions.SubscribeOutputDeviceFailedException(ex, provider, subReq);
            }
        }

        public bool UnsubscribeOutput(OutputSubscriptionRequest _subReq)
        {
            UnsubscribeOutputWithExceptions(_subReq);
            return true;
        }

        public void UnsubscribeOutputWithExceptions(OutputSubscriptionRequest _subReq)
        {
            var subReq = _subReq.Clone();
            LogOutputSubReq("UnsubscribeOutput", subReq);
            if (ActiveOutputSubscriptions.ContainsKey(subReq.SubscriptionDescriptor.SubscriberGuid))
            {
                var provider = GetProvider<IOutputProvider>(subReq.ProviderDescriptor.ProviderName);
                try
                {
                    provider.UnSubscribeOutputDevice(subReq);
                    ActiveOutputSubscriptions.Remove(subReq.SubscriptionDescriptor.SubscriberGuid);
                }
                catch (Exception ex)
                {
                    throw new IOControllerExceptions.UnsubscribeOutputDeviceFailedException(ex, provider, subReq);
                }
            }
        }

        private void LogOutputSubReq(string title, OutputSubscriptionRequest subReq)
        {
            Log("{0}: Provider {1}, Device {2}, Guid {3}", 
                title,
                subReq.ProviderDescriptor.ProviderName, 
                subReq.DeviceDescriptor.DeviceHandle, 
                subReq.SubscriptionDescriptor.SubscriberGuid);
        }

        // ToDo: Renaame to SetOutputState
        public bool SetOutputstate(OutputSubscriptionRequest subReq, BindingDescriptor bindingDescriptor, int state)
        {
            SetOutputStateWithExceptions(subReq, bindingDescriptor, state);
            return true;
        }

        public void SetOutputStateWithExceptions(OutputSubscriptionRequest subReq, BindingDescriptor bindingDescriptor, int state)
        {
            var provider = GetProvider<IOutputProvider>(subReq.ProviderDescriptor.ProviderName);
            try
            {
                provider.SetOutputState(subReq, bindingDescriptor, state);
            }
            catch (Exception ex)
            {
                throw new IOControllerExceptions.SetOutputStateFailedException(ex, provider, subReq, bindingDescriptor);
            }
        }

        public void RefreshProviderLiveState(string providerName)
        {
            var provider = GetProvider(providerName);
            provider.RefreshLiveState();
        }

        public bool IsProviderLive(string providerName)
        {
            var provider = GetProvider(providerName);
            return provider.IsLive;
        }

        public void RefreshDevices()
        {
            foreach (var provider in _providers.Values)
            {
                provider.RefreshDevices();
            }
        }

        public void RefreshDevices(string providerName)
        {
            var provider = GetProvider(providerName);
            provider.RefreshDevices();
        }

        public IProvider GetProvider(string providerName)
        {
            if (!_providers.ContainsKey(providerName)) throw new IOControllerExceptions.ProviderNotFoundException($"Provider {providerName} Not found");
            return _providers[providerName];
        }

        public TInterface GetProvider<TInterface>(IProvider provider)
        {
            if (provider is TInterface returnedProvider)
            {
                return returnedProvider;
            }
            throw new IOControllerExceptions.ProviderDoesNotSupportInterfaceException(provider, typeof(TInterface));
        }

        public TInterface GetProvider<TInterface>(string providerName)
        {
            var provider = GetProvider(providerName);
            return GetProvider<TInterface>(provider);
        }
    }
}
