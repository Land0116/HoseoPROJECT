public interface IDamageable
    {
        // OnDamage 메서드는 입력으로 데미지 크기(damage)를 받는다
        // Death 메서드는 몬스터 또는 플레이어가 죽는다.
        public void OnDamage(float damage);
        public void Death();
    }
