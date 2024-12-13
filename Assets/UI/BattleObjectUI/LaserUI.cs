using UnityEngine;

public class LaserUI : BattleObjectUI {
    [SerializeField] private SpriteRenderer startHighlight;
    [SerializeField] private SpriteRenderer endHighlight;

    private Laser laser;
    private bool fireing;

    public override void Setup(BattleObject battleObject, UIManager uIManager) {
        base.Setup(battleObject, uIManager);
        laser = (Laser)battleObject;
        spriteRenderer.enabled = false;
        startHighlight.enabled = false;
        endHighlight.enabled = false;
        transform.localScale = new Vector2(laser.laserTurret.laserTurretScriptableObject.laserSize, 1);
        startHighlight.transform.localScale = new Vector2(.2f, .2f);
        endHighlight.transform.localScale = new Vector2(.2f, .2f);
    }

    public override void UpdateObject() {
        base.UpdateObject();
        if (laser.fireing) {
            if (!fireing) {
                startHighlight.enabled = uIManager.GetEffectsShown();
            }
            fireing = true;

            transform.localPosition = new Vector2(0, 0);
            transform.rotation = transform.parent.rotation;
            spriteRenderer.size = new Vector2(spriteRenderer.size.x, laser.laserLength);
            transform.Translate(Vector2.up * (laser.laserLength / 2 + laser.laserTurret.GetTurretOffSet()));

            if (laser.hitPoint != null) {
                endHighlight.transform.localPosition = new Vector2(0, laser.laserLength / 2);
                endHighlight.enabled = uIManager.GetEffectsShown();
            } else {
                endHighlight.enabled = false;
            }

            startHighlight.transform.localPosition = new Vector2(0, -laser.laserLength / 2);

            if (laser.fireTime > 0) {
                spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.b, spriteRenderer.color.g, .8f);
                startHighlight.color = new Color(startHighlight.color.r, startHighlight.color.b, startHighlight.color.g, .8f);
                endHighlight.color = new Color(endHighlight.color.r, endHighlight.color.b, endHighlight.color.g, 1);
            } else {
                spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.b, spriteRenderer.color.g,
                    laser.fadeTime / laser.laserTurret.GetFireDuration());
                startHighlight.color = new Color(startHighlight.color.r, startHighlight.color.b, startHighlight.color.g,
                    laser.fadeTime / laser.laserTurret.GetFadeDuration());
                endHighlight.color = new Color(endHighlight.color.r, endHighlight.color.b, endHighlight.color.g,
                    laser.fadeTime / laser.laserTurret.GetFadeDuration());
            }
        } else if (!laser.fireing && fireing) {
            fireing = false;
            startHighlight.enabled = false;
            endHighlight.enabled = false;
        }
    }

    public void ShowEffects(bool shown) {
        startHighlight.enabled = fireing && shown;
        endHighlight.enabled = fireing && shown;
    }
}
