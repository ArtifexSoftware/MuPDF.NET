using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BarcodeWriter.Core.Internal
{
	internal class ProfileManager : IDisposable
	{
		private object _managedClass;
		private readonly List<Profile> _profiles = new List<Profile>();
        
        internal IEnumerable<Profile> Profiles => _profiles;

		public ProfileManager(Object managedClass)
		{
			_managedClass = managedClass;

			// Load profiles from resources
			try
			{
                Assembly assembly = Assembly.GetExecutingAssembly();
                string[] resourceNames = assembly.GetManifestResourceNames();
                foreach (string resourceName in resourceNames)
                {
                    if (resourceName.EndsWith("profiles.json"))
                    {
                        using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                            if (stream != null)
                                using (StreamReader reader = new StreamReader(stream))
                                    ParseProfiles(reader.ReadToEnd());
                    }
                }
			}
			catch (Exception exception)
			{
				throw new BarcodeProfileException("Profiles parsing error.", exception);
			}
		}
        
        public void Dispose()
        {
            _managedClass = null;
            _profiles.Clear();
        }

		public void LoadFromFile(string fileName)
		{
			try
			{
				ParseProfiles(File.ReadAllText(fileName));
			}
			catch (Exception exception)
			{
				throw new BarcodeProfileException("Profiles parsing error.", exception);
			}
		}
        
        internal void LoadFromString(string jsonString)
        {
            try
            {
                ParseProfiles(jsonString);
            }
            catch (Exception exception)
            {
                throw new BarcodeProfileException("Profiles parsing error.", exception);
            }
        }

        internal void LoadAndApplyProfiles(string jsonString)
        {
            try
            {
                _profiles.Clear();
                ParseProfiles(jsonString);
                foreach (Profile profile in _profiles)
                    ApplyProfile(profile.Name);
            }
            catch (Exception exception)
            {
                throw new BarcodeProfileException("Profiles parsing error.", exception);
            }
        }

        private void ParseProfiles(string jsonString)
        {
            void addProfile(IJEnumerable<JToken> jEnumerable, string profileName)
            {
                Profile profile = new Profile { Name = profileName };

                foreach (JToken jProp in jEnumerable)
                {
                    string propertyName = ((JProperty) jProp).Name;

                    if (propertyName.ToLower() == "keywords")
                    {
                        IJEnumerable<JToken> jKeywords = ((JProperty) jProp).Value.AsJEnumerable();
                        foreach (JToken jKeyword in jKeywords)
                            profile.Keywords.Add(jKeyword.ToString());

                        if (profile.Keywords.Count > 0)
                            profile.Deferred = true;
                    }
                    else
                    {
                        JToken valueToken = ((JProperty) jProp).Value;
                        string strValue;

                        if (valueToken is JValue)
                            strValue = ((JValue) valueToken).ToString(CultureInfo.InvariantCulture);
                        else
                            strValue = valueToken.ToString();
                        
                        profile.Properties.Add(new KeyValuePair<string, string>(propertyName, strValue));
                    }
                }

                int existing = _profiles.FindIndex(p => p.Name == profile.Name);
                if (existing > -1)
                    _profiles.RemoveAt(existing);

                _profiles.Add(profile);
            }


            JObject o = JObject.Parse(jsonString);
            IJEnumerable<JToken> jProfiles = o.GetValue("profiles").AsJEnumerable();

            if (jProfiles != null)
            {
                foreach (JToken jProfile in jProfiles)
                    addProfile(((JProperty) jProfile.First).Value.AsJEnumerable(), ((JProperty) jProfile.First).Name);
            }
            else // simplified profile
            {
                addProfile(o.AsJEnumerable(), "profile1");
            }
        }

		public void ApplyProfiles(string profiles)
		{
			string[] profileNames = profiles.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string profile in profileNames)
				ApplyProfile(profile.Trim());
		}
        
        internal void ApplyProfile(string profileName)
        {
            foreach (Profile profile in _profiles)
            {
                if (string.Compare(profileName, profile.Name, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    profile.Selected = true;
                    if (!profile.Deferred)
                        ApplyProfile(profile);
                    return;
                }
            }

            throw new BarcodeProfileException($"Unknown profile '{profileName}'.");
        }

        internal void ApplyProfile(Profile profile)
		{
            foreach (KeyValuePair<string, string> argument in profile.Properties)
            {
                if (string.Compare(argument.Key, "Description", StringComparison.InvariantCultureIgnoreCase) == 0)
                    continue;

                // Split by dot assuming we can have a chain,
                // e.g. `MidProperty1.MidProperty2.Property` or `MidProperty1.MidProperty2.Method()`
                string[] propertyChain = argument.Key.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                object @object = _managedClass;
                Type type = _managedClass.GetType();

                for (int i = 0; i < propertyChain.Length; i++)
                {
                    string memberName = propertyChain[i];

                    // recurse mid properties
                    if (i < propertyChain.Length - 1)
                    {
                        PropertyInfo propertyInfo = type.GetProperty(memberName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                        if (propertyInfo == null)
                            throw new BarcodeProfileException($"Property \"{memberName}\" does not exist in \"{type}\"");

                        @object = propertyInfo.GetValue(@object, null);
                        type = @object.GetType();
                    }
                    else // process the last part of the chain: a property or method
                    {
                        // .Method()
                        if (memberName.EndsWith("()", StringComparison.Ordinal))
                        {
                            string methodName = memberName.Substring(0, memberName.Length - 2);

                            if (string.IsNullOrEmpty(argument.Value))
                            {
                                MethodInfo methodInfo = type.GetMethod(methodName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy, null, new Type[0], null);
                                if (methodInfo == null)
                                    throw new BarcodeProfileException($"Method \"{methodName}\" does not exist in \"{type}\"");

                                try
                                {
                                    methodInfo.Invoke(@object, null);
                                }
                                catch (Exception exception)
                                {
                                    throw new BarcodeProfileException($"Could not invoke method \"{argument.Key}\"", exception);
                                }
                            }
                            else
                            {
                                JArray argsArray = null;
                                MethodInfo methodInfo = null;

                                try
                                {
                                    argsArray = JArray.Parse(argument.Value.Trim());
                                }
                                catch (JsonReaderException exception)
                                {
                                    throw new BarcodeProfileException(
                                        $"Could not parse arguments of the method \"{argument.Key}\"", exception);
                                }

                                foreach (var mi in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
                                {
                                    if (string.Equals(mi.Name, methodName, StringComparison.InvariantCultureIgnoreCase) && 
                                        mi.GetParameters().Length == argsArray.Count)
                                    {
                                        methodInfo = mi;
                                        break;
                                    }
                                }

                                if (methodInfo == null)
                                    throw new BarcodeProfileException(
                                        $"Could not find method \"{argument.Key}\" with {argsArray.Count} arguments.");

                                var methodParameters = methodInfo.GetParameters();
                                object[] arrParams = new object[argsArray.Count];

                                for (int p = 0; p < methodParameters.Length; p++)
                                {
                                    var typeConverter = TypeDescriptor.GetConverter(methodParameters[p].ParameterType);
                                    arrParams[p] = typeConverter.ConvertFromInvariantString(argsArray[p].Value<string>());
                                }

                                try
                                {
                                    methodInfo.Invoke(@object, arrParams);
                                }
                                catch (Exception exception)
                                {
                                    throw new BarcodeProfileException($"Could not invoke method \"{argument.Key}\"", exception);
                                }
                            }
                        }
                        else // .Property=Value
                        {
                            PropertyInfo propertyInfo = type.GetProperty(memberName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                            if (propertyInfo == null)
                                throw new BarcodeProfileException($"Property \"{memberName}\" does not exist in \"{type}\"");

                            try
                            {
                                if (propertyInfo.PropertyType.BaseType == typeof(Array))
                                {
                                    string classTypeName = propertyInfo.PropertyType.FullName.Trim(new[] { '[', ']' });
                                    Type classType = Type.GetType(classTypeName);
                                    TypeConverter typeConverter = TypeDescriptor.GetConverter(classType);

                                    if (!string.IsNullOrEmpty(argument.Value))
                                    {
                                        string args = argument.Value.Trim();
                                        args = args.Trim(new[] { '[', ']' });
                                        string[] parts = Regex.Split(args, ", *");

                                        Array array = Array.CreateInstance(classType, parts.Length);

                                        for (int v = 0; v < parts.Length; v++)
                                        {
                                            string piece = parts[v];
                                            array.SetValue(typeConverter.ConvertFromInvariantString(piece.Trim()), v);
                                        }

                                        propertyInfo.SetValue(@object, array, BindingFlags.FlattenHierarchy, null, null,
                                            CultureInfo.InvariantCulture);
                                    }
                                    else
                                        propertyInfo.SetValue(@object, null, BindingFlags.FlattenHierarchy, null, null,
                                            CultureInfo.InvariantCulture);
                                }
                                else
                                {
                                    TypeConverter typeConverter = TypeDescriptor.GetConverter(propertyInfo.PropertyType);
                                    object value = typeConverter.ConvertFromInvariantString(argument.Value);
                                    propertyInfo.SetValue(@object, value, BindingFlags.FlattenHierarchy, null, null,
                                        CultureInfo.InvariantCulture);
                                }
                            }
                            catch (Exception exception)
                            {
                                throw new BarcodeProfileException(
                                    $"Could not set value \"{argument.Value}\" to property \"{argument.Key}\"", exception);
                            }
                        }
                    }
                }
            }
        }
    }
    
    internal class Profile
    {
        internal string Name { get; set; } = null;

        internal List<KeyValuePair<string, string>> Properties { get; set; } = new List<KeyValuePair<string, string>>();

        internal bool Selected { get; set; } = false;
        internal bool Deferred { get; set; } = false;

        internal List<string> Keywords { get; set; } = new List<string>();
        
        public Profile()
        {
        }
    }
}

namespace BarcodeWriter.Core
{
#if BARCODESDK_EMBEDDED_SOURCES
	internal class BarcodeProfileException : BarcodeException
#else
    public class BarcodeProfileException : BarcodeException
#endif
    {
        public BarcodeProfileException(string message) : base(message)
        {
        }

        public BarcodeProfileException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
