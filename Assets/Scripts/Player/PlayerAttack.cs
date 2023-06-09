using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    public WeaponParameters _weaponParameters {get; private set;}
    private Transform _hands;
    private Transform _weapon;
    private Transform _fireSpot;

    public bool fireContinuously {get; set;}
    public float lastFireTime {get; set;}
    private float _flowStartTime;
    private float _maxFlowTime = 3f;
    private float _bulletSpeed = 20f;
    private bool _dead = false;

    
    private void Awake() {
        _hands = transform.Find("Hands");
        _hands.Find(GameUtils.weapon).gameObject.SetActive(true);
        lastFireTime = Time.time;
        DetectWeapon();
    }
    
    private void FixedUpdate() {
        if (fireContinuously) {
            if (Time.time - _flowStartTime < _maxFlowTime) FireBullet();
            else fireContinuously = false;
        }
    }

    private bool CanAttack() {
        float timeSinceLastFire = Time.time - lastFireTime;
        if (timeSinceLastFire >= _weaponParameters.timeBetweenAttacks && !_dead) return true;
        return false;
    }

    private void DetectWeapon() {
        foreach (Transform child in _hands.transform) {
            if (child.gameObject.activeSelf) {
                _weapon = child;
            }
        }
        _weaponParameters = _weapon.GetComponent<WeaponParameters>();
        if (_weapon.Find("FireSpot")) {
            _fireSpot = _weapon.Find("FireSpot");
        }
    }

    private void FireBullet() {
        lastFireTime = Time.time;
        _weapon.GetComponent<Animator>().SetTrigger("shoot");
        GameObject bullet = Instantiate(_weaponParameters.bulletPrefab, _fireSpot.transform.position, new Quaternion());
        bullet.GetComponent<Rigidbody2D>().velocity = _bulletSpeed * _fireSpot.transform.right;
    }

    private void FireMultipleBullets() {
        lastFireTime = Time.time;
        _weapon.GetComponent<Animator>().SetTrigger("shoot");
        Vector3 dir = _fireSpot.transform.right;
        foreach (int i in new int[]{-1,0,1}) {
            GameObject bullet = Instantiate(_weaponParameters.bulletPrefab, _fireSpot.transform.position, new Quaternion());
            bullet.GetComponent<Rigidbody2D>().velocity = _bulletSpeed * (Quaternion.AngleAxis(i*10, transform.forward) * dir);
        }
    }

    private void Attack() {
        _weapon.GetComponent<Animator>().SetTrigger("attack");
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(
            _hands.position,
            _weaponParameters.attackRange
        );
        foreach (Collider2D enemy in hitEnemies) {
            if (enemy.tag.ToLower().Contains("enemy")) {
                enemy.GetComponent<EnemyAttack>().EnemyDeath(); 
            }
        }
    }

    private void OnDrawGizmos() {
        if (_hands) Gizmos.DrawWireSphere(_hands.position, _weaponParameters.attackRange);
    }

    private void OnFire(InputValue inputValue) {
        DetectWeapon();
        switch (_weaponParameters.attackMethod) {
            case WeaponParameters.FireMethods.single:
                if (CanAttack()) FireBullet();
                break;
            case WeaponParameters.FireMethods.multiple:
                if (CanAttack()) FireMultipleBullets();
                break;
            case WeaponParameters.FireMethods.non_stop:
                if (CanAttack()) {
                    _flowStartTime = Time.time;
                    fireContinuously = inputValue.isPressed;
                }
                break;
            case WeaponParameters.FireMethods.white:
                if (CanAttack()) Attack();
                break;
            default: break;
        }
    }

    public void PlayerDeath() {
        _dead = true;
        GetComponent<PlayerMovement>().enabled = false;
        GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        transform.Find("Hands").gameObject.SetActive(false);
        GameUtils.DieAnimations(gameObject, GetComponent<Animator>());

        Destroy(gameObject, 2f);    
    }

    private void OnDestroy() {
        GameObject.Find("Pause").GetComponent<PauseMenu>().GameOver();
    }
}