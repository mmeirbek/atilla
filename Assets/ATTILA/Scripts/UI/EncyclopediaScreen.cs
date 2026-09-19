using UnityEngine;
using UnityEngine.UI;
namespace ATTILA.UI
{
 public sealed class EncyclopediaScreen:MonoBehaviour
 {
  public static void Show(Transform root){var h=new GameObject("Encyclopedia Screen");h.transform.SetParent(root,false);h.AddComponent<EncyclopediaScreen>().Build();}
  private void Build(){MenuUiKit.Background(transform);MenuUiKit.Header(transform,"ЭНЦИКЛОПЕДИЯ",()=>Destroy(gameObject));Column("ТҰЛҒАЛАР",new[]{"Абылай хан","Қабанбай батыр"},.25f);Column("ОҚИҒАЛАР",new[]{"Тарихи оқиға","🔒"},.5f);Column("МӘДЕНИЕТ",new[]{"Домбыра","Ұлттық киім","Ұлттық ойындар"},.75f);}
  private void Column(string title,string[] e,float x){var p=MenuUiKit.Panel(transform,new Vector2(x,.51f),new Vector2(390,560));var h=UiFactory.Text(p.transform,title,25,TextAnchor.MiddleCenter,MenuUiKit.Gold);h.fontStyle=FontStyle.Bold;UiFactory.SetRect(h.rectTransform,new Vector2(.5f,.89f),new Vector2(340,42),Vector2.zero);for(int i=0;i<e.Length;i++){var locked=e[i]=="🔒";var c=MenuUiKit.Card(p.transform,e[i],locked?"ЖАБЫҚ":"Уақытша карта",new Vector2(.5f,.68f-i*.23f),new Vector2(320,105),null,!locked);c.interactable=false;}}
 }
}
