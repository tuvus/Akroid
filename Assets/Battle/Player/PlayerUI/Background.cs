using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Background : MonoBehaviour {
	SpriteRenderer spriteRenderer;

	public void SetupBackground() {
		spriteRenderer = GetComponent<SpriteRenderer>();
    }
	public void UpdateBackground(float size, float transparency) {
		transform.localPosition = new Vector3(0, 0, 110);
		gameObject.transform.localScale = new Vector3(size, size, 0);
		spriteRenderer.color = new Color(1, 1, 1, transparency);

	}
}
