using System;

namespace CDG.Save
{
    /// <summary>
    /// 저장 데이터를 구분하기 위한 슬롯 이름을 나타냅니다.
    /// 생성된 SaveSlot은 항상 유효한 이름을 가지며 저장 경로 자체는 노출하지 않습니다.
    /// </summary>
    public sealed class SaveSlot : IEquatable<SaveSlot>
    {
        /// <summary>
        /// 슬롯 이름으로 허용되는 최대 길이입니다.
        /// </summary>
        public const int MaxLength = 64;

        /// <summary>
        /// 기본 저장 슬롯입니다.
        /// </summary>
        public static SaveSlot Default { get; } = new SaveSlot("default");

        /// <summary>
        /// 저장 슬롯 이름입니다.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 지정한 이름으로 저장 슬롯을 생성합니다.
        /// 이름은 영문 소문자, 숫자, 하이픈(-), 밑줄(_)만 사용할 수 있습니다.
        /// </summary>
        /// <param name="name">저장 슬롯 이름입니다.</param>
        /// <exception cref="ArgumentException">이름이 비어 있거나 허용 규칙을 위반한 경우 발생합니다.</exception>
        public SaveSlot(string name)
        {
            ValidateName(name);
            Name = name;
        }

        /// <summary>
        /// 다른 SaveSlot과 동일한 슬롯 이름을 가지는지 비교합니다.
        /// </summary>
        public bool Equals(SaveSlot other)
        {
            if (other is null)
            {
                return false;
            }

            return string.Equals(Name, other.Name, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is SaveSlot other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Name);
        }

        /// <summary>
        /// 슬롯 이름을 문자열로 반환합니다.
        /// </summary>
        public override string ToString()
        {
            return Name;
        }

        public static bool operator ==(SaveSlot left, SaveSlot right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left is null || right is null)
            {
                return false;
            }

            return left.Equals(right);
        }

        public static bool operator !=(SaveSlot left, SaveSlot right)
        {
            return !(left == right);
        }

        private static void ValidateName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("저장 슬롯 이름은 비어 있을 수 없습니다.", nameof(name));
            }

            if (name.Length > MaxLength)
            {
                throw new ArgumentException($"저장 슬롯 이름은 {MaxLength}자를 초과할 수 없습니다.", nameof(name));
            }

            for (int i = 0; i < name.Length; i++)
            {
                if (!IsAllowedCharacter(name[i]))
                {
                    throw new ArgumentException("저장 슬롯 이름에는 영문 소문자, 숫자, 하이픈(-), 밑줄(_)만 사용할 수 있습니다.", nameof(name));
                }
            }
        }

        private static bool IsAllowedCharacter(char character)
        {
            return character >= 'a' && character <= 'z'
                || character >= '0' && character <= '9'
                || character == '-'
                || character == '_';
        }
    }
}