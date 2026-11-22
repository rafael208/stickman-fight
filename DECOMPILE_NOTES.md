# Разбор Supreme Duelist Stickman (декомпиляция)

> Изучено для понимания механик (учебный проект). Не содержит и не копирует код/ассеты оригинала.
> Метод: il2cppdumper v6.7.46 по `libil2cpp.so` + `global-metadata.dat` (Unity **2022.3.62f3**, IL2CPP).
> Ограничение: тела методов — нативный код (обфусцирован), поэтому читаются **структура классов, имена полей и все строки**, но не сами формулы.

## Итог

Оригинал — это **10-телый активный рэгдолл на HingeJoint2D + JointMotor2D**, управляемый **покомпонентно**: у каждой части тела свой «балансер» со своей силой, скоростью восстановления и станом. Мой подход (сервопривод суставов через мотор + ослабление при стане) архитектурно совпадает с их решением.

## Структура рэгдолла

| Часть (их имя) | Мой эквивалент |
|---|---|
| `tete` / `HeadRb` — голова | `Head` |
| `corpsR` / `Body` — корпус | `Torso` |
| `BrasR[]` / `Arms` — плечи ×2 | `UpperArmL/R` |
| `mainsR[]` / `Hands` — кисти/предплечья ×2 | `ForearmL/R` |
| `JambesR[]` / `Legs` — ноги ×2 | `ThighL/R` |
| `PiedsR[]` / `Feet` — ступни ×2 | `ShinL/R` |

Шарниры: `BodyJoint` (корпус), `LegJoint1/2`, `FootHinge`, `corp`, `jambesJ1/J2` — всё `HingeJoint2D`.

## Система баланса (ключевое)

На каждую часть тела вешается компонент-балансер:

- **`Equilibre`**: `rb2D` (часть), `revSpeed` (скорость восстановления), `stun` (оглушение), `rot` (целевой угол), `jump` (импульс).
- **`S_PlayerBalancer`**: `Stun`, `RevSpeed`, приватные `jag` (float — усиление), `jah` (Rigidbody2D).
- **`Jambe` / `S_PlayerLeg`**: `JointMotor2D jambe1`, `anglejambe` (целевой угол), `MaxSpeed` (предел скорости мотора), `POWER`, `RevSpeed`, `State`.
- **`Pied` / `S_PlayerFoot`**: `powerJump` (сила прыжка).

Контроллер персонажа (`PlayerDirection` / `S_PlayerMovement`) держит общие параметры:
`balanceForce` (сила баланса), `followForce` (следование за целью), `dirForce` (сила по направлению), `balanceGoalSpeed` (целевая скорость), `GravityForce`, `JumpForce`, `JumpTime`.

**Вывод о механике:** баланс = «тянуть каждую часть к целевой позе» через мотор/силу, с параметрами `balanceForce` + `RevSpeed`, и ослаблять через `Stun` при попадании. Это и есть активный рэгдолл.

## Бой и отбрасывание

- **`CollisionWeapon`** (ближний бой): урон зависит от того, **какой частью тела ударили** — `chocpowerWeapon`, `chocpowerTete` (голова), `chocpowerMember`, `chocpowerSmallMember`. Плюс `DAMAGE`, `TimeReCollision` (кулдаун попадания), `attenuation` (затухание).
- **Здоровье** `CharHP`: `HeadMulti` (множитель урона по голове!), `maxHealth`, `allRb`.
- **Отбрасывание**: `knockbackForce`, `knockback_Y`, `dirKnockBack`, `selfKnockback` (отдача при выстреле), `selfKnockbackWater`, `waterKnockBack`, `punchKnockBack`, `KnockBackLifetime`.
- **Используют `PointEffector2D`** (`impact`, `KnockBack`) — зоны импульса/взрыва через встроенный компонент Unity, а не руками.
- Оружие: `ArmeObject` (ScriptableObject), `GunWeapon`, `KnockBackWeapon`, `CollisionWeapon`, `GiantWeapon`.

## Что перенести в наш проект (конкретно)

1. **Множитель урона по части тела** (у нас `headDamageMultiplier` объявлен, но не подключён!) — подключить: урон ×1.5 при попадании в голову.
2. **Per-part hit power** — разные `chocpower` для оружия/головы/крупной/мелкой конечности.
3. **`PointEffector2D` для нокбэка/взрывов** — молот и снаряды создают зону импульса (проще и «физичнее»).
4. **`selfKnockback`** — отдача при выстреле из пистолета.
5. **`followForce`/`dirForce`** в балансе — тянуть корпус в сторону движения/цели силой, а не только моментом.
6. **Покомпонентный баланс** (у меня сейчас общий `recoverRampTime` + сервопривод) — вынести `RevSpeed`/`Stun`/`POWER` на каждый сустав, как у них.

## Что НЕ удалось извлечь

- Сами формулы и числовые значения (balanceForce = сколько, revSpeed = сколько) — они в нативном коде и бинарных prefab-ассетах (`assets/bin/Data/*.assets`). Числа можно вытащить через **AssetStudio**, но это отдельный заход.
- Имена части методов обфусцированы (`imr`, `jtl`, `lee`…), тела — ассемблер.
