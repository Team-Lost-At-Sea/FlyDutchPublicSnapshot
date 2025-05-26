using UnityEngine;

public class BeerDrank : MonoBehaviour
{
    public ActiveItem beerActiveItem;
    public string drinkAnimationName = "BeerGulp";

    void Start()
    {
        beerActiveItem = GetComponent<ActiveItem>();
        // Get the ActiveItem component from this GameObject's parent
        beerActiveItem = beerActiveItem ?? GetComponentInParent<ActiveItem>();
        beerActiveItem.OnAttack += PlayBeerDrinkAnimation;
    }
    private void PlayBeerDrinkAnimation(int itemIDPleaseDoNotChange)
    {
        Animator beerAnim = GetComponent<Animator>();
        beerAnim.Play(drinkAnimationName, -1, 0);
    }
}
