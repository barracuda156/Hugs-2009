using System;

namespace HsWrapGen
{
	/// <summary>
	/// 
	/// </summary>
	public class HsOutput
	{
        private System.Type m_type;
		private System.Reflection.MemberInfo[] m_members;
        private System.Collections.Specialized.StringCollection m_names;
        private System.Collections.Specialized.StringCollection m_imports;

		public HsOutput(System.Type ty,System.Reflection.MemberInfo[] mems)
		{
            m_type = ty;
			m_members = mems;
            m_names   = new System.Collections.Specialized.StringCollection();
            m_imports = new System.Collections.Specialized.StringCollection();
		}

        protected void OutputHeader(System.IO.StreamWriter st) {
            st.WriteLine("module {0} where", m_type.FullName);
            st.WriteLine("");
            st.WriteLine("import DotNet");
            st.WriteLine("import qualified {0}", m_type.BaseType.FullName);
            foreach (String s in m_imports) {
              st.WriteLine("import {0}", s);
            }
            st.WriteLine("");
            st.WriteLine("data {0}_ a", m_type.Name);
            st.WriteLine("type {0} a = {1}.{2} ({0}_ a)",
                         m_type.Name,
                         m_type.BaseType.FullName,
                         m_type.BaseType.Name);
            st.WriteLine("");
        }

        private String ToHaskellName(String x) {
            System.String candName, candNameOrig;
            System.Int32 uniq = 1;
            if (System.Char.IsUpper(x[0])) {
                candName = 
                    String.Concat(System.Char.ToLower(x[0]),
                    x.Substring(1));
            } else {
                candName = x;
            }
            candNameOrig = candName;
            while (m_names.Contains(candName)) {
                candName = String.Concat(candNameOrig,"_",uniq.ToString());
                uniq++;
            }
            m_names.Add(candName);

            return candName;
        }

        private void AddImport(System.String nm) {

            if (!m_imports.Contains(nm)) {
                m_imports.Add(nm);
            }
        }

        protected void OutputHaskellType(System.Text.StringBuilder sb,
                                         System.Type ty,
                                         System.Int32 idx) {
            if (ty.FullName == "System.Int32") {
              sb.Append("Int"); return;
            }
            if (ty.FullName == "System.Boolean") {
              sb.Append("Bool"); return;
            }
            if (ty.FullName == "System.String") {
                sb.Append("String"); return;
            }
            if (ty.FullName == "System.Char") {
              sb.Append("Char"); return;
            }
            if (ty.FullName == "System.Void") {
              sb.Append("()"); return;
            }
            if (ty.FullName == "System.Object") {
                sb.AppendFormat("System.Object.Object a{0}",idx); return;
            }

            if (ty.IsArray) {
                String eltNm = ty.GetElementType().FullName;
                AddImport("System.Array");
                AddImport(eltNm);
                sb.AppendFormat("System.Array ({0}.{1} a{2})", eltNm, ty.GetElementType().Name, idx);
            } else {
                AddImport(ty.FullName);
                sb.AppendFormat("{0}.{1} a{2}", ty.FullName, ty.Name, idx);
            }       
        }

        protected void OutputMethodSig(System.Text.StringBuilder sb,
                                       System.Reflection.MemberInfo mi) {
            System.Reflection.MethodInfo m = (System.Reflection.MethodInfo)mi;
            System.Reflection.ParameterInfo[] ps = m.GetParameters();
            int i;

            for (i=0; i < ps.Length; i++) {
                OutputHaskellType(sb,ps[i].ParameterType,i);
                sb.Append(" -> ");
            }
            sb.AppendFormat("{0} obj -> IO (", mi.DeclaringType.Name);
            OutputHaskellType(sb,m.ReturnType,i);
            sb.AppendFormat("){0}",System.Environment.NewLine);
        }

        protected void OutputArgs(System.Text.StringBuilder sb,
                                  System.Reflection.MemberInfo mi,
                                  System.Boolean isTupled) {
            System.Reflection.MethodInfo m = (System.Reflection.MethodInfo)mi;
            Int32 i = 0;
            System.Reflection.ParameterInfo[] ps = m.GetParameters();

            if (isTupled && ps.Length != 1) sb.Append("(");

            for (i=0; i < ps.Length; i++) {
                sb.AppendFormat("arg{0}",i); 
                if (isTupled && (i+1) < ps.Length) {
                    sb.Append(",");
                } else {
                    if (!isTupled) sb.Append(" ");
                }
            }
            if (isTupled && ps.Length != 1) sb.Append(")");
        }

        protected void OutputMember(System.Text.StringBuilder sb,
            System.Reflection.MemberInfo mi) {
            switch (mi.MemberType) {
                case System.Reflection.MemberTypes.Method:
                    System.String methName = ToHaskellName(mi.Name);
                    sb.AppendFormat("{0} :: ", methName);
                    OutputMethodSig(sb,mi);
                    sb.AppendFormat("{0} ", methName);
                    OutputArgs(sb,mi,false);
                    sb.AppendFormat(" = invoke \"{0}\" ", mi.Name);
                    OutputArgs(sb,mi,true);
                    // the mind boggles, System.Environment ?
                    sb.AppendFormat("{0}",System.Environment.NewLine);
                    break;
                default:
                    break;
            }
        }
        
        public void OutputToFile(String fn) {
            System.IO.FileStream fs = new System.IO.FileStream(fn,System.IO.FileMode.Create);
            System.IO.StreamWriter st = new System.IO.StreamWriter(fs,System.Text.Encoding.ASCII);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            foreach (System.Reflection.MemberInfo mem in m_members) {
                OutputMember(sb,mem);
            }
 
            OutputHeader(st);
            st.WriteLine(sb.ToString());
            st.Flush();
            st.Close();
            fs.Close();
        }
	}
}
