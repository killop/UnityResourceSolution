namespace Daihenka.AssetPipeline
{
    internal static class EditorTextures
    {
        static EditorTexture s_TriangleDown;
        static EditorTexture s_TriangleUp;
        static EditorTexture s_X;

        public static EditorTexture Down
        {
            get
            {
                if (s_TriangleDown == null)
                {
                    s_TriangleDown = new EditorTexture(34, 34,
                        "iVBORw0KGgoAAAANSUhEUgAAACIAAAAiCAYAAAA6RwvCAAAA/ElEQVRYCe3VMQ7CMAyFYcrKxm04Ir0GXA9G1uJ/QEJWQp6dpUiu1KHB7zV8RWXZtu2wh+O4h02wh9qIfxIlUiJewF//1W/kZrvnrZc9r/7bt64X4c16tuCjFRbXTjb3Gs0qj+ZpJfdRUefz1daHmyCriDCXVZE0uIEiwlxGRdbgBqoIs1EVWYNyVYTZiEpIg/KICPOqSkiD4ogI84pKWIPiqAiZkUpYg9KoCJlfKikNSjMi5HoqKQ0KMyLkWippDQqzImS9SlqDsqwI2W+VKQ3KZkTIf1SmNCiaESGPysVO6R+WQO+YFen1htdnRcI37AVqI16mRErEC/jrN6YOVdWgTQ39AAAAAElFTkSuQmCC");
                }

                return s_TriangleDown;
            }
        }

        public static EditorTexture Up
        {
            get
            {
                if (s_TriangleUp == null)
                {
                    s_TriangleUp = new EditorTexture(34, 34,
                        "iVBORw0KGgoAAAANSUhEUgAAACIAAAAiCAYAAAA6RwvCAAABGklEQVRYCe2WQQrCQAxFraAL3bnWhbrSE3gEPYN4A+/gYfRMutOFXkEXCtYfaKGEoUlmKnSRgaFNJpP/+zqUZnmed9owum0wQR7cCH8TTsSJcAI89jPyLyIjNKYZPbKGPvHHwsEu1kkTRsYQvxcGJrg+Ysw0cVhPFeHqfSUt36YSWUDizGSWiC8sJ4apRq5QmDKVG+IZy4lhyqtZoTs3QYKUozXTSCESolGKE5U5pvqvK5bIBiIhGqURWluXgeYaQyRD46+mOWroQVVUYohslSaoTF1rJdJD87fBCJX2MT/SHiuRvdQwsK7aYyEygMgzIKRJDVH0qiu0EDnUNRLWxL0WIoJW2rKFSJqSsNuNcEBOxIlwAjxuzRn5AeVIM5pI9JapAAAAAElFTkSuQmCC");
                }

                return s_TriangleUp;
            }
        }

        public static EditorTexture X
        {
            get
            {
                if (s_X == null)
                {
                    s_X = new EditorTexture(34, 34,
                        "iVBORw0KGgoAAAANSUhEUgAAACIAAAAiCAYAAAA6RwvCAAABTklEQVRYCe2WUQ7CIAyGnaeaR9CbeY7dwee96a1mf0IT0rVd2RaDCU0I0vKXj7I5hmVZLi3YtQUIMHQQeRK9InsqMpBooobXa5QJnDHmQgMtcviG19dpA8VmaqXdaeBpEMOc0pADuUydGchCJNDMg5EQrHdhPJCJMxi9BmNBcIon/VDXVJ15Mou9voTZguA86prp3IynCA/b24iV7kcevEqn8ftG/o8ay7tXKSkW3SXv1uvL6q3WWzkUsDNgXAisGQHBnCMwmxA1IHthQhC1ILUwYQiANPPRiz4jtdWgTSYLVyUKgoR7LQQTATkCwfCbMFsgZ0CEYP7iL5534vVlyaPVU09BddLK8DdzDdBuZ1ydshJyM1ZlZhKbtzSZRI41GA+C9RLGhcAJsNDrAcPHNEIUbJgLg9asBMVSvjRBvaj82NnMt6aDyJPvFZEV+QLtPDH5milIBAAAAABJRU5ErkJggg==");
                }

                return s_X;
            }
        }
    }
}