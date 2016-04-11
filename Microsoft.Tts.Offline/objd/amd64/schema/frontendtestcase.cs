﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.8669
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// 
// This source code was auto-generated by xsd, Version=2.0.50727.42.
// 
namespace Microsoft.Tts.Offline.Schema {
    using System.Xml.Serialization;
    
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://schemas.microsoft.com/tts")]
    [System.Xml.Serialization.XmlRootAttribute("cases", Namespace="http://schemas.microsoft.com/tts", IsNullable=false)]
    public partial class CasesType {
        
        private CaseType[] caseField;
        
        private string langField;
        
        private ComponentType componentField;
        
        private bool componentFieldSpecified;
        
        private CategoryType categoryField;
        
        private bool categoryFieldSpecified;
        
        private string frequencyField;
        
        private string sayasField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("case")]
        public CaseType[] @case {
            get {
                return this.caseField;
            }
            set {
                this.caseField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute(DataType="language")]
        public string lang {
            get {
                return this.langField;
            }
            set {
                this.langField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ComponentType component {
            get {
                return this.componentField;
            }
            set {
                this.componentField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool componentSpecified {
            get {
                return this.componentFieldSpecified;
            }
            set {
                this.componentFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public CategoryType category {
            get {
                return this.categoryField;
            }
            set {
                this.categoryField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool categorySpecified {
            get {
                return this.categoryFieldSpecified;
            }
            set {
                this.categoryFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string frequency {
            get {
                return this.frequencyField;
            }
            set {
                this.frequencyField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string sayas {
            get {
                return this.sayasField;
            }
            set {
                this.sayasField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://schemas.microsoft.com/tts")]
    public partial class CaseType {
        
        private string commentField;
        
        private string inputField;
        
        private PartType[] outputField;
        
        private PartType[] acceptField;
        
        private PriorityType priorityField;
        
        private bool priorityFieldSpecified;
        
        private string sourceField;
        
        private string categoryField;
        
        private string pron_polywordField;
        
        private string tn_sayasField;
        
        private string tn_formatField;
        
        private string pron_sayasField;
        
        private string pron_formatField;
        
        private string indexField;
        
        /// <remarks/>
        public string comment {
            get {
                return this.commentField;
            }
            set {
                this.commentField = value;
            }
        }
        
        /// <remarks/>
        public string input {
            get {
                return this.inputField;
            }
            set {
                this.inputField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("part", IsNullable=false)]
        public PartType[] output {
            get {
                return this.outputField;
            }
            set {
                this.outputField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("part", IsNullable=false)]
        public PartType[] accept {
            get {
                return this.acceptField;
            }
            set {
                this.acceptField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public PriorityType priority {
            get {
                return this.priorityField;
            }
            set {
                this.priorityField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool prioritySpecified {
            get {
                return this.priorityFieldSpecified;
            }
            set {
                this.priorityFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string source {
            get {
                return this.sourceField;
            }
            set {
                this.sourceField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string category {
            get {
                return this.categoryField;
            }
            set {
                this.categoryField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string pron_polyword {
            get {
                return this.pron_polywordField;
            }
            set {
                this.pron_polywordField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string tn_sayas {
            get {
                return this.tn_sayasField;
            }
            set {
                this.tn_sayasField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string tn_format {
            get {
                return this.tn_formatField;
            }
            set {
                this.tn_formatField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string pron_sayas {
            get {
                return this.pron_sayasField;
            }
            set {
                this.pron_sayasField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string pron_format {
            get {
                return this.pron_formatField;
            }
            set {
                this.pron_formatField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string index {
            get {
                return this.indexField;
            }
            set {
                this.indexField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://schemas.microsoft.com/tts")]
    public partial class PartType {
        
        private string wordField;
        
        private BreakOptionType breakOptionField;
        
        private bool breakOptionFieldSpecified;
        
        private string valueField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string word {
            get {
                return this.wordField;
            }
            set {
                this.wordField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public BreakOptionType breakOption {
            get {
                return this.breakOptionField;
            }
            set {
                this.breakOptionField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool breakOptionSpecified {
            get {
                return this.breakOptionFieldSpecified;
            }
            set {
                this.breakOptionFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value {
            get {
                return this.valueField;
            }
            set {
                this.valueField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://schemas.microsoft.com/tts")]
    public enum BreakOptionType {
        
        /// <remarks/>
        must,
        
        /// <remarks/>
        no,
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://schemas.microsoft.com/tts")]
    public enum PriorityType {
        
        /// <remarks/>
        P0,
        
        /// <remarks/>
        P1,
        
        /// <remarks/>
        P2,
        
        /// <remarks/>
        P3,
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://schemas.microsoft.com/tts")]
    public enum ComponentType {
        
        /// <remarks/>
        SentenceSeparator,
        
        /// <remarks/>
        WordBreaker,
        
        /// <remarks/>
        TextNormalization,
        
        /// <remarks/>
        Pronunciation,
        
        /// <remarks/>
        SentenceTypeDetector,
        
        /// <remarks/>
        ProsodicBreak,
        
        /// <remarks/>
        BoundaryTone,
        
        /// <remarks/>
        NonUniform,
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://schemas.microsoft.com/tts")]
    public enum CategoryType {
        
        /// <remarks/>
        Rule,
        
        /// <remarks/>
        POS,
        
        /// <remarks/>
        Nothing,
    }
}
