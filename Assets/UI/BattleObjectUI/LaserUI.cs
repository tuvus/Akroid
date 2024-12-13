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
                if (uIManager.GetEffectsShown()) {
                    startHighlight.enabled = true;
                    endHighlight.enabled = true;
                }
                spriteRenderer.enabled = true;
            }
            fireing = true;

            transform.localPosition = new Vector2(0, 0);
            transform.rotation = transform.parent.rotation;
            transform.localScale = laser.scale;

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
            spriteRenderer.enabled = false;
            startHighlight.enabled = false;
            endHighlight.enabled = false;
        }
    }

    public void ShowEffects(bool shown) {
        startHighlight.enabled = fireing && shown;
        endHighlight.enabled = fireing && shown;
    }
}
