using System.Text;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures.Types;

namespace ZZZRPCDumper.Parser
{
    internal class FieldType
    {
        public string FieldName;
        public string Type;
        public bool IsGeneric;
        public List<FieldType> GenericFields = null;
        public bool IsEnum;
        public List<KeyValuePair<string, string>> EnumFields = null;

        public FieldType(string fieldName, string fieldType, List<FieldType> genericFields = null, List<KeyValuePair<string, string>> enumFields = null)
        {
            FieldName = fieldName;
            Type = fieldType;

            if (genericFields != null)
            {
                IsGeneric = true;
                GenericFields = genericFields;
            }

            if (enumFields != null)
            {
                IsEnum = true;
                EnumFields = enumFields;
            }
        }
        public FieldType(string fieldType, List<FieldType> genericFields = null, List<KeyValuePair<string, string>> enumFields = null)
        {
            FieldName = null;
            Type = fieldType;

            if (genericFields != null)
            {
                IsGeneric = true;
                GenericFields = genericFields;
            }

            if (enumFields != null)
            {
                IsEnum = true;
                EnumFields = enumFields;
            }
        }

        public static string NormalizeQWERName(string name)
        {
            if (name.StartsWith("CPropertyLinkedListModule") || name.StartsWith("CPLinkedList"))
                return "LinkedList";
            else if (name.StartsWith("CPropertyListModule") || name.StartsWith("CPList"))
                return "List";
            else if (name.StartsWith("CPropertyHashSetModule") || name.StartsWith("CPHashSet"))
                return "HashSet";
            else if (name.StartsWith("CPropertyDictionaryModule") || name.StartsWith("CPDictionary"))
                return "Dictionary";
            else if (name.StartsWith("CPolymorphsim"))
                return "CPolymorphsim";
            else if (name.StartsWith("Dictionary`2"))
                return "Dict";
            else if (name.StartsWith("HashSet`1"))
                return "HashSet";
            else if (name.StartsWith("List`1"))
                return "List";
            else if (name.StartsWith("CData`1"))
                return "CData";
            else
                return name;

        }

        public static List<FieldType> GetGenericTypes(IList<TypeSignature> types, RPCParser rpcInst)
        {
            var genericTypes = new List<FieldType>();
            foreach (var type in types)
            {
                if (type.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.Rows.ElementType.GenericInst && (type.Namespace.StartsWith("QWER") || type.Namespace.StartsWith("Share") || type.Name.StartsWith("CPolymorphsim") || type.Name.StartsWith("Dictionary") || type.Name.StartsWith("HashSet")))
                {
                    var genericType = (GenericInstanceTypeSignature)type;
                    var nestedGenericTypes = new List<FieldType>();

                    foreach (var genTypes in genericType.TypeArguments)
                    {
                        if(rpcInst != null)
                        {
                            if (genTypes.FullName.StartsWith("Share."))
                                rpcInst.TypeToRPCType(genTypes.Resolve());

                            if ((from intf in genTypes.Resolve().Interfaces where intf.Interface.Name == "IPolymorphsimObject" select intf).Count() > 0)
                            {
                                IEnumerable<TypeDefinition> polyTypeList = from polyType in rpcInst.ManifestModule.TopLevelTypes where polyType.BaseType != null && polyType.BaseType.Name == genTypes.Name select polyType;
                                foreach (TypeDefinition polyType in polyTypeList)
                                {
                                    rpcInst.TypeToRPCType(polyType);
                                }
                            }
                        }
                        nestedGenericTypes.Add(new FieldType(genTypes.FullName));
                    }

                    var field = new FieldType(NormalizeQWERName(type.Name), nestedGenericTypes);
                    genericTypes.Add(field);
                }
                else
                {
                    if (rpcInst != null)
                    {
                        if (type.FullName.StartsWith("Share."))
                            rpcInst.TypeToRPCType(type.Resolve());

                        if ((from intf in type.Resolve().Interfaces where intf.Interface.Name == "IPolymorphsimObject" select intf).Count() > 0)
                        {
                            IEnumerable<TypeDefinition> polyTypeList = from polyType in rpcInst.ManifestModule.TopLevelTypes where polyType.BaseType != null && polyType.BaseType.Name == type.Name select polyType;
                            foreach (TypeDefinition polyType in polyTypeList)
                            {
                                rpcInst.TypeToRPCType(polyType);
                            }
                        }
                    }
                    genericTypes.Add(new FieldType(type.FullName));
                }
            }
            return genericTypes;
        }
        public static FieldType CreateFromBasicType(PropertyDefinition prop, IList<TypeSignature> types, RPCParser rpcInst = null)
        {
            return new FieldType(prop.Name, types[0].FullName);
        }
        public static FieldType CreateFromType(PropertyDefinition prop, TypeSignature type, RPCParser rpcInst = null)
        {
            return new FieldType(prop.Name, type.FullName);
        }
        public static FieldType CreateFromPoly(PropertyDefinition prop, IList<TypeSignature> types, RPCParser rpcInst = null)
        {
            var genericTypes = GetGenericTypes(types, rpcInst);
            return new FieldType(prop.Name, "CPolymorphsim", genericTypes);
        }
        public static FieldType CreateFromCData(PropertyDefinition prop, IList<TypeSignature> types, RPCParser rpcInst = null)
        {
            var genericTypes = GetGenericTypes(types, rpcInst);
            return new FieldType(prop.Name, "CData", genericTypes);
        }
        public static FieldType CreateFromCPropBlob(PropertyDefinition prop, IList<TypeSignature> types, RPCParser rpcInst = null)
        {
            var genericTypes = GetGenericTypes(types, rpcInst);
            return new FieldType(prop.Name, "CPropertyBlob", genericTypes);
        }
        public static FieldType CreateFromList(PropertyDefinition prop, IList<TypeSignature> types, RPCParser rpcInst = null)
        {
            var genericTypes = GetGenericTypes(types, rpcInst);
            return new FieldType(prop.Name, "List", genericTypes);
        }

        public static FieldType CreateFromLinkedList(PropertyDefinition prop, IList<TypeSignature> types, RPCParser rpcInst = null)
        {
            var genericTypes = GetGenericTypes(types, rpcInst);
            return new FieldType(prop.Name, "LinkedList", genericTypes);
        }

        internal static FieldType CreateFromHashSet(PropertyDefinition prop, IList<TypeSignature> types, RPCParser rpcInst = null)
        {
            var genericTypes = GetGenericTypes(types, rpcInst);
            return new FieldType(prop.Name, "HashSet", genericTypes);
        }
        public static FieldType CreateFromDict(PropertyDefinition prop, IList<TypeSignature> types, RPCParser rpcInst = null)
        {
            var genericTypes = GetGenericTypes(types, rpcInst);
            return new FieldType(prop.Name, "Dict", genericTypes);
        }
        public static FieldType CreateFromDoubleDict(PropertyDefinition prop, IList<TypeSignature> types, RPCParser rpcInst = null)
        {
            var genericTypes = GetGenericTypes(types, rpcInst);
            return new FieldType(prop.Name, "DoubleDict", genericTypes);
        }
    }

    internal class RPC
    {
        public string Name;
        public ushort ID;
        public string BaseType;
        public NestedRPC CArg;
        public NestedRPC CRet;
        public NestedRPC CRetExt;
        public List<KeyValuePair<string, string>> Fields;
        public List<FieldType> FieldTypes;

        public RPC(string name, ushort id, string baseType, NestedRPC cArg, NestedRPC cRet, NestedRPC cRetExt, List<KeyValuePair<string, string>> fields)
        {
            Name = name;
            ID = id;
            BaseType = baseType;
            CArg = cArg;
            CRet = cRet;
            CRetExt = cRetExt;
            Fields = fields;
        }

        public RPC(string name, ushort id, string baseType, NestedRPC cArg, NestedRPC cRet, NestedRPC cRetExt, List<FieldType> fields)
        {
            Name = name;
            ID = id;
            BaseType = baseType;
            CArg = cArg;
            CRet = cRet;
            CRetExt = cRetExt;
            FieldTypes = fields;
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"{Name}:");
            if (ID != 0) stringBuilder.AppendLine($"ID: {ID}");
            stringBuilder.AppendLine($"BaseType: {BaseType}");

            if (Fields.Count > 0)
            {
                stringBuilder.AppendLine("Fields:");
                foreach (var field in Fields)
                {
                    stringBuilder.AppendLine($"  {field.Key} - {field.Value}");
                }
            }

            if (CArg != null)
            {
                stringBuilder.AppendLine();
                stringBuilder.AppendLine("CArg:");
                foreach (var field in CArg.Fields)
                {
                    stringBuilder.AppendLine($"  {field.Key} - {field.Value}");
                }
            }

            if (CRet != null)
            {
                stringBuilder.AppendLine();
                stringBuilder.AppendLine("CRet:");
                foreach (var field in CRet.Fields)
                {
                    stringBuilder.AppendLine($"  {field.Key} - {field.Value}");
                }
            }

            if (CRetExt != null)
            {
                stringBuilder.AppendLine();
                stringBuilder.AppendLine("CRetExt:");
                foreach (var field in CRetExt.Fields)
                {
                    stringBuilder.AppendLine($"  {field.Key} - {field.Value}");
                }
            }

            return stringBuilder.ToString();
        }
    }

    internal class NestedRPC
    {
        public string BaseType;
        public List<KeyValuePair<string, string>> Fields;
        public List<FieldType> FieldTypes;
        public NestedRPC(string baseType, List<KeyValuePair<string, string>> fields)
        {
            BaseType = baseType;
            Fields = fields;
        }

        public NestedRPC(string baseType, List<FieldType> fields)
        {
            BaseType = baseType;
            FieldTypes = fields;
        }
    }
}