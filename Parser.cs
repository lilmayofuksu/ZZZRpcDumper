using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;

namespace ZZZRPCDumper.Parser
{
    internal class RPCParser
    {

        private string AssemblyPath;
        private AssemblyDefinition Assembly;
        public ModuleDefinition ManifestModule;

        public Dictionary<string, RPC> RPCs;
        public Dictionary<string, RPC> RPCTypes;

        public RPCParser(string assemblyPath)
        {
            AssemblyPath = assemblyPath;
        }

        public Dictionary<string, RPC> Parse()
        {
            Assembly = AssemblyDefinition.FromFile(AssemblyPath);
            ManifestModule = Assembly.ManifestModule;

            RPCs = new Dictionary<string, RPC>();
            RPCTypes = new Dictionary<string, RPC>();

            foreach (var type in ManifestModule.TopLevelTypes)
            {
                if (type.Namespace == "Share")
                {
                    var rpc = TypeToRPC(type);
                    if (rpc != null) RPCs.Add(type.FullName, rpc);
                }
            }
            return RPCs;
        }

        private RPC TypeToRPC(TypeDefinition type)
        {
            ushort id = 0;
            string name = type.Name;

            foreach (var field in type.Fields)
            {
                if (field.Name == "ID" && field.Signature.FieldType == ManifestModule.CorLibTypeFactory.UInt16)
                    id = (ushort)field.Constant.Value.InterpretData(field.Constant.Type);
                if (field.Name == "Name" && field.Signature.FieldType == ManifestModule.CorLibTypeFactory.String)
                    name = (string)field.Constant.Value.InterpretData(field.Constant.Type);
            }

            NestedRPC cArg = null;
            NestedRPC cRet = null;
            NestedRPC cRetExt = null;

            foreach (var nestedType in type.NestedTypes)
            {
                if (nestedType.Name == "CArg")
                {
                    cArg = TypeToNestedRPC(nestedType);
                }
                else if (nestedType.Name == "CRet")
                {
                    cRet = TypeToNestedRPC(nestedType);
                }
                else if (nestedType.Name == "CRetExt")
                {
                    cRetExt = TypeToNestedRPC(nestedType);
                }
            }

            var fields = new List<KeyValuePair<string, string>>();

            foreach (var property in type.Properties)
            {
                fields.Add(new KeyValuePair<string, string>(property.Name, property.Signature.ReturnType.FullName));
            }

            if (id != 0) return new RPC(name, id, type.BaseType.FullName == "System.Object" ? null : type.BaseType.FullName, cArg, cRet, cRetExt, fields);
            return null;
        }

        private void ResolveGenericTypes(IList<AsmResolver.DotNet.Signatures.Types.TypeSignature> types)
        {
            foreach (var type in types)
            {
                if(type.ElementType == ElementType.GenericInst)
                {
                    var genericType = (AsmResolver.DotNet.Signatures.Types.GenericInstanceTypeSignature)type;
                    ResolveGenericTypes(genericType.TypeArguments);
                }
                else if(type.Namespace == "Share")
                {
                    TypeToRPCType(type.Resolve());
                }
            }
        }

        private FieldType ResolvePropType(PropertyDefinition property)

        {
            FieldType fieldType = null;

            if (property.Signature.ReturnType.ElementType == ElementType.GenericInst)
            {
                var genericType = (AsmResolver.DotNet.Signatures.Types.GenericInstanceTypeSignature)property.Signature.ReturnType;
                ResolveGenericTypes(genericType.TypeArguments);

                if (property.Signature.ReturnType.Name.StartsWith("CPropertyBasicTypeModule") && genericType.TypeArguments.Count == 1)
                {
                    fieldType = FieldType.CreateFromBasicType(property, genericType.TypeArguments, this);
                }
                else if (property.Signature.ReturnType.Name.StartsWith("CPropertyListModule") || property.Signature.ReturnType.Name.StartsWith("CPList") || property.Signature.ReturnType.Name.StartsWith("List"))
                {
                    fieldType = FieldType.CreateFromList(property, genericType.TypeArguments, this);
                }
                else if (property.Signature.ReturnType.Name.StartsWith("CPropertyDictionaryModule") || property.Signature.ReturnType.Name.StartsWith("CPDictionary") || property.Signature.ReturnType.Name.StartsWith("Dictionary"))
                {
                    fieldType = FieldType.CreateFromDict(property, genericType.TypeArguments, this);
                }
                else if (property.Signature.ReturnType.Name.StartsWith("CPropertyHashSetModule") || property.Signature.ReturnType.Name.StartsWith("CPHashSet") || property.Signature.ReturnType.Name.StartsWith("HashSet"))
                {
                    fieldType = FieldType.CreateFromHashSet(property, genericType.TypeArguments, this);
                }
                else if (property.Signature.ReturnType.Name.StartsWith("CPropertyDKDictionaryModule") || property.Signature.ReturnType.Name.StartsWith("CPDKDictionary") || property.Signature.ReturnType.Name.StartsWith("CDoubleKeyDictionary"))
                {
                    fieldType = FieldType.CreateFromDoubleDict(property, genericType.TypeArguments, this);
                }
                else if (property.Signature.ReturnType.Name.StartsWith("CPropertyLinkedListModule") || property.Signature.ReturnType.Name.StartsWith("CPLinkedList"))
                {
                    fieldType = FieldType.CreateFromLinkedList(property, genericType.TypeArguments, this);
                }
                else if (property.Signature.ReturnType.Name.StartsWith("CPolymorphsim"))
                {
                    fieldType = FieldType.CreateFromPoly(property, genericType.TypeArguments, this);
                }
                else if (property.Signature.ReturnType.Name.StartsWith("CData"))
                    fieldType = FieldType.CreateFromCData(property, genericType.TypeArguments, this);

                else if (property.Signature.ReturnType.Name.StartsWith("CPropertyBlob"))
                    fieldType = FieldType.CreateFromCPropBlob(property, genericType.TypeArguments, this);

                else
                    fieldType = FieldType.CreateFromType(property, property.Signature.ReturnType, this);
            }
            else
            {
                if (property.Signature.ReturnType.Namespace == "Share")
                {
                    TypeToRPCType(property.Signature.ReturnType.Resolve());
                }

                fieldType = FieldType.CreateFromType(property, property.Signature.ReturnType, this);
            }

            return fieldType;
        }

        public void TypeToRPCType(TypeDefinition type)
        {
            ushort id = 0;
            string name = type.Name;

            foreach (var field in type.Fields)
            {
                if (field.Name == "ID" && field.Signature.FieldType == ManifestModule.CorLibTypeFactory.UInt16)
                    id = (ushort)field.Constant.Value.InterpretData(field.Constant.Type);
                if (field.Name == "Name" && field.Signature.FieldType == ManifestModule.CorLibTypeFactory.String)
                    name = (string)field.Constant.Value.InterpretData(field.Constant.Type);
            }

            NestedRPC cArg = null;
            NestedRPC cRet = null;
            NestedRPC cRetExt = null;


            var fieldTypes = new List<FieldType>();

            foreach (var property in type.Properties)
            {
                fieldTypes.Add(ResolvePropType(property));
            }

            if (type.IsEnum)
            {
                var enumFields = new List<KeyValuePair<string, string>>();
                foreach (var enums in type.Fields)
                {
                    if (enums.Constant != null)
                    {
                        if (enums.Constant.Type == ElementType.I2)
                        {
                            short enumVal = (short)enums.Constant.Value.InterpretData(ElementType.I2);
                            enumFields.Add(new KeyValuePair<string, string>($"{enums.Name}", enumVal.ToString()));
                        }
                        else if (enums.Constant.Type == ElementType.U1)
                        {
                            byte enumVal = (byte)enums.Constant.Value.InterpretData(ElementType.U1);
                            enumFields.Add(new KeyValuePair<string, string>($"{enums.Name}", enumVal.ToString()));
                        }
                        else if (enums.Constant.Type == ElementType.U2)
                        {
                            ushort enumVal = (ushort)enums.Constant.Value.InterpretData(ElementType.U2);
                            enumFields.Add(new KeyValuePair<string, string>($"{enums.Name}", enumVal.ToString()));
                        }
                    }
                }
                fieldTypes.Add(new FieldType("Enum", $"System.Enum::{type.GetEnumUnderlyingType().FullName}", null, enumFields) );
            }

            var subRpc = new RPC(name, id, type.BaseType.FullName == "System.Object" ? null : type.BaseType.FullName, cArg, cRet, cRetExt, fieldTypes);

            if(type.IsEnum)
            {
                subRpc.BaseType = $"System.Enum::{type.GetEnumUnderlyingType().FullName}";
            }

            if (subRpc != null && (type.FullName.ToString().StartsWith("Share.CPtc") || type.FullName.ToString().StartsWith("Share.CRpc")))
            {
                if (!RPCs.ContainsKey(type.FullName))
                    RPCs.Add(type.FullName, subRpc);
                return;
            }
            else if (subRpc != null && !RPCTypes.ContainsKey(type.FullName))
                RPCTypes.Add(type.FullName, subRpc);
        }


        private NestedRPC TypeToNestedRPC(TypeDefinition type)
        {
            if (type.Fields.Count > 0)
            {
                var fields = new List<FieldType>();
                foreach (var property in type.Properties)
                {
                    var fieldName = property.Name.ToString();
                    if (fieldName != "ProtocolID" && fieldName != "ProtocolName")
                    {
                        fields.Add(ResolvePropType(property));
                        if (!property.Signature.ReturnType.FullName.StartsWith("System."))
                        {
                            if (property.Signature.ReturnType.FullName.StartsWith("Share.CPropertyBlob`1") || property.Signature.ReturnType.FullName.StartsWith("Share.CData`1"))
                            {
                                var genericTypes = ((AsmResolver.DotNet.Signatures.Types.GenericInstanceTypeSignature)property.Signature.ReturnType).TypeArguments;
                                foreach (var genType in genericTypes)
                                {
                                    TypeToRPCType(genType.Resolve());
                                    Console.WriteLine(genType.FullName);
                                }
                            }
                            else
                            {
                                //var subRpc = TypeToRPC(property.Signature.ReturnType.Resolve());
                                Console.WriteLine(property.Signature.ReturnType.FullName);
                                //if (subRpc != null)
                                    //RPCs.Add(property.Signature.ReturnType.FullName, subRpc);
                               // else
                                    TypeToRPCType(property.Signature.ReturnType.Resolve());
                            }
                        }
                    }
                }

                return fields.Count > 0 ? new NestedRPC(type.BaseType.FullName == "System.Object" ? null : type.BaseType.FullName, fields) : null;
            }
            return null;
        }
    }
}