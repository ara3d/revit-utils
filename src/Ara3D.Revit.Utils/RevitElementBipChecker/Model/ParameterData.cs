using Autodesk.Revit.DB;

namespace RevitElementBipChecker.Model
{
   public class ParameterData
    {
        public ParameterData(Parameter parameter,Document doc,bool isinstance=true)
        {
            Parameter = parameter;
            BuiltInParameter = (parameter.Definition as InternalDefinition).BuiltInParameter.ToString();
            ParameterName = parameter.Definition.Name;
            Id = parameter.Id.ToString();
            ParameterGroup = parameter.Definition.ParameterGroup.ToString();
            ParameterType = parameter.GetParameterType();
            GroupName = LabelUtils.GetLabelFor(parameter.Definition.ParameterGroup);
            Type = parameter.GetParameterType();
            ReadWrite = parameter.IsReadWrite();
            Value = parameter.GetValue();
            StringValue = parameter.AsValueString() == null
                ? parameter.AsString()
                : parameter.AsValueString() ;
            Shared = parameter.Shared();
            GUID = parameter.Guid();
            TypeOrInstance = isinstance?"Instance":"Type";
            AssGlobalPara = parameter.GetAssGlobalParameter(doc);
            AssGlobalParaValue = parameter.GetAssGlobalParameterValue(doc);
        }

        public Parameter Parameter { get; set; }
        public string ParameterName { get; set; }
        public string Id { get; set; }
        public string TypeOrInstance { get; set; }
        public string BuiltInParameter { get; set; }
        public string Type { get; set; }
        public string ReadWrite { get; set; }
        public string Value { get; set; }
        public string StringValue { get; set; }
        public string ParameterGroup { get; set; }
        public string ParameterType { get; set; }
        public string GroupName { get; set; }
        public string Shared { get; set; }
        public string GUID { get; set; }

        public string AssGlobalPara { get; set; }
        public string AssGlobalParaValue { get; set; }
        
    }
}
