namespace TestApplication
{
    public class StructParamClass
    {
        public struct MyStruct
        {
            public int IntVal { get; set; }
        }

        public void RunStructs()
        {
            MyStruct x = StructReturn();
            StructIn(x);
            StructOut(out x);
        }

        public MyStruct StructReturn()
        {
            return new MyStruct() {IntVal = 1};
        }
        public void StructIn(MyStruct inp)
        {
        }

        public void StructOut(out MyStruct inp)
        {
            inp = new MyStruct() { IntVal = 1 };
        }

    }
}
