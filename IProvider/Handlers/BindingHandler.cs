﻿using Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Providers.Handlers
{
    public abstract class BindingHandler<TPoll>
    {
        private InputSubscriptionRequest tmpSubReq;

        public abstract bool Subscribe(InputSubscriptionRequest subReq);

        public abstract void Poll(TPoll pollValue);
    }
}
