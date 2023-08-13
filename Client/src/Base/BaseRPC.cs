using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Carbon.Client.Base;
using Carbon.Extensions;

namespace Carbon.Client.Base
{
	public class BaseRPC
	{
		public CarbonClientPlugin Plugin { get; set; }

		public Il2CppSystem.Type EntityType { get; set; }

		public string Name { get; set; }
        public uint ID { get; set; }

		public bool InstanceRPC { get; set; }

        public Func<object, BaseEntity.RPCMessage, bool> Callback { get; set; }
	}
}