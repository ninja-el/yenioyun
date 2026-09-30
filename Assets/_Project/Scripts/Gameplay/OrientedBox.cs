using UnityEngine;

namespace MatchPack.Gameplay
{
    /// <summary>
    /// Dünya uzayında döndürülmüş kutu. Yığında aynı turda yer ayrılan objelerin collider'ı henüz
    /// kapalı olduğu için birbirleriyle çakışmaları fizik yerine bu kutularla test edilir.
    /// </summary>
    public readonly struct OrientedBox
    {
        // Neredeyse paralel eksenlerde çapraz çarpım sıfıra yaklaşır; bu pay sayısal hatayla
        // çakışan kutuların ayrık sayılmasını engeller.
        private const float ParallelEpsilon = 1e-5f;

        public readonly Vector3 Center;
        public readonly Vector3 HalfExtents;
        public readonly Quaternion Rotation;

        /// <summary>Kutuyu her dönüşte saran kürenin yarıçapı; pahalı testten önce hızlı eleme için.</summary>
        public readonly float Radius;

        public OrientedBox(Vector3 center, Vector3 halfExtents, Quaternion rotation)
        {
            Center = center;
            HalfExtents = halfExtents;
            Rotation = rotation;
            Radius = halfExtents.magnitude;
        }

        /// <summary>Verilen dönüşteki kutuyu saran eksen hizalı kutunun yarı ölçüsü.</summary>
        public static Vector3 GetAlignedExtents(Vector3 halfExtents, Quaternion rotation)
        {
            Matrix4x4 matrix = Matrix4x4.Rotate(rotation);
            Vector3 result = Vector3.zero;

            for (int row = 0; row < 3; row++)
            {
                result[row] = Mathf.Abs(matrix[row, 0]) * halfExtents.x
                    + Mathf.Abs(matrix[row, 1]) * halfExtents.y
                    + Mathf.Abs(matrix[row, 2]) * halfExtents.z;
            }

            return result;
        }

        /// <summary>Ayırıcı eksen testi (15 eksen). Yalnızca değen kutular çakışmış sayılmaz.</summary>
        public bool Intersects(in OrientedBox other)
        {
            float radiusSum = Radius + other.Radius;
            if ((other.Center - Center).sqrMagnitude >= radiusSum * radiusSum) { return false; }

            Quaternion inverse = Quaternion.Inverse(Rotation);
            Matrix4x4 r = Matrix4x4.Rotate(inverse * other.Rotation);
            Vector3 t = inverse * (other.Center - Center);
            Vector3 a = HalfExtents;
            Vector3 b = other.HalfExtents;

            Matrix4x4 absR = Matrix4x4.zero;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    absR[i, j] = Mathf.Abs(r[i, j]) + ParallelEpsilon;
                }
            }

            for (int i = 0; i < 3; i++)
            {
                float rb = b.x * absR[i, 0] + b.y * absR[i, 1] + b.z * absR[i, 2];
                if (Mathf.Abs(t[i]) > a[i] + rb) { return false; }
            }

            for (int j = 0; j < 3; j++)
            {
                float ra = a.x * absR[0, j] + a.y * absR[1, j] + a.z * absR[2, j];
                float distance = t.x * r[0, j] + t.y * r[1, j] + t.z * r[2, j];
                if (Mathf.Abs(distance) > ra + b[j]) { return false; }
            }

            for (int i = 0; i < 3; i++)
            {
                int i1 = (i + 1) % 3;
                int i2 = (i + 2) % 3;

                for (int j = 0; j < 3; j++)
                {
                    int j1 = (j + 1) % 3;
                    int j2 = (j + 2) % 3;

                    float ra = a[i1] * absR[i2, j] + a[i2] * absR[i1, j];
                    float rb = b[j1] * absR[i, j2] + b[j2] * absR[i, j1];
                    float distance = t[i2] * r[i1, j] - t[i1] * r[i2, j];
                    if (Mathf.Abs(distance) > ra + rb) { return false; }
                }
            }

            return true;
        }
    }
}
