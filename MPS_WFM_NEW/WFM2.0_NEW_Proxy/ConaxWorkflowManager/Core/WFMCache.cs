using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Runtime.Caching;
using log4net;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core
{
    public class WFMCache
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly ObjectCache Cache = MemoryCache.Default;
        private static Double DefaultTTL = 15;

        public static Object Get(String key)  
        {
            try {
                return Cache[key] as Object;
            }
            catch
            {
                return null;
            }
        }

        public static T Get<T>(String key) where T : class
        {
            try
            {
                var obj = Cache[key];

                if (obj == null)
                    return null;

                var clone = ObjectCloner.Clone((T)obj);

                return clone;
            }
            catch (Exception ex) {
                var type = typeof(T);
                log.Warn("Failed to read object of type " + type + " from cache.");
                return null;
            }
        }

        public static void Add<T>(String key, T objectToCache) where T : class
        {
            Cache.Add(key, objectToCache, DateTime.Now.AddMinutes(DefaultTTL));
        }

        public static void Add<T>(String key, T objectToCache, DateTime absExp) where T : class
        {
            Cache.Add(key, objectToCache, absExp);
        }
        /*
        public static void Add(String key, Object objectToCache)
        {
            Cache.Add(key, objectToCache, DateTime.Now.AddSeconds(30));
        }

        public static void Add(String key, Object objectToCache, DateTime absExp)
        {
            Cache.Add(key, objectToCache, absExp);
        }
        */
        public static void Clear(String key)
        {
            Cache.Remove(key);
        }
        
        public static bool Exists(String key)
        {
            return Cache.Get(key) != null;
        }
        
        public static List<String> GetAll()
        {
            return Cache.Select(keyValuePair => keyValuePair.Key).ToList();
        }
    }
}
