﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hidwizards.IOWrapper.Libraries.DeviceLibrary;
using HidWizards.IOWrapper.DataTransferObjects;
using HidWizards.IOWrapper.ProviderInterface.Interfaces;
using NAudio.Midi;

namespace Core_Midi
{
    [Export(typeof(IProvider))]
    public class Core_Midi : IInputProvider
    {
        private readonly MidiIn _midiIn;
        private readonly IInputDeviceLibrary<string> _deviceLibrary;

        public Core_Midi()
        {
            _deviceLibrary = new MidiDeviceLibrary(new ProviderDescriptor { ProviderName = ProviderName });
            _midiIn = new MidiIn(0);
            _midiIn.MessageReceived += midiIn_MessageReceived;
            _midiIn.Start();
        }

        private void midiIn_MessageReceived(object sender, MidiInMessageEventArgs e)
        {
            var _update = EventToBindingUpdate(e);
            if (_update == null) return;
            var update = (BindingUpdate)_update;

            //Console.WriteLine($"Channel: {e.MidiEvent.Channel}, Event: {e.MidiEvent}");
            Console.WriteLine($"Index: {update.Binding.Index}, SubIndex: {update.Binding.SubIndex}, Value: {update.Value}");
        }

        public BindingUpdate? EventToBindingUpdate(MidiInMessageEventArgs e)
        {
            var update = new BindingUpdate{Binding = new BindingDescriptor()};
            switch (e.MidiEvent.CommandCode)
            {
                case MidiCommandCode.NoteOn:
                case MidiCommandCode.NoteOff:
                    var note = (NoteEvent)e.MidiEvent;
                    update.Binding.SubIndex = note.NoteNumber;
                    update.Value = e.MidiEvent.CommandCode == MidiCommandCode.NoteOn ? note.Velocity : 0;
                    update.Value = ConvertAxis127(update.Value);
                    break;
                case MidiCommandCode.ControlChange:
                    var cc = (ControlChangeEvent)e.MidiEvent;
                    update.Binding.SubIndex = (int)cc.Controller;
                    update.Value = cc.ControllerValue;
                    update.Value = ConvertAxis127(update.Value);
                    break;
                case MidiCommandCode.PitchWheelChange:
                    var pw = (PitchWheelChangeEvent)e.MidiEvent;
                    update.Binding.SubIndex = 0;
                    update.Value = ConvertPitch(pw.Pitch);
                    break;
                default:
                    return null;
            }

            var index = e.RawMessage & 0xff;
            if (e.MidiEvent.CommandCode == MidiCommandCode.NoteOff) index += 16;    // Convert NoteOff to NoteOn
            update.Binding.Index = index;
            return update;
        }

        // Converts an Axis in the range 0..127 to signed 16-bit int
        private int ConvertAxis127(int value)
        {
            return (int)(value * 516.0236220472441) - 32768;
        }

        private int ConvertPitch(int value)
        {
            return (int) (value * 4.000183116645303) - 32768;    // ToDo: Center point seems to be 1.
        }

        public void Dispose()
        {
            
        }

        public string ProviderName { get; } = "Core_Midi";
        public bool IsLive { get; }
        public void RefreshLiveState()
        {
            throw new NotImplementedException();
        }

        public void RefreshDevices()
        {
            throw new NotImplementedException();
        }

        public ProviderReport GetInputList()
        {
            return _deviceLibrary.GetInputList();
        }

        public DeviceReport GetInputDeviceReport(InputSubscriptionRequest subReq)
        {
            throw new NotImplementedException();
        }

        public bool SubscribeInput(InputSubscriptionRequest subReq)
        {
            throw new NotImplementedException();
        }

        public bool UnsubscribeInput(InputSubscriptionRequest subReq)
        {
            throw new NotImplementedException();
        }
    }
}
