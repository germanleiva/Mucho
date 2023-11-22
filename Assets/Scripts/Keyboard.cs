//Unity boilerplate
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Keyboard : MonoBehaviour
{

    public TMPro.TMP_InputField inputField;

    public Button key_a, key_b, key_c, key_d, key_e, key_f, key_g, key_h, key_i, key_j, key_k, key_l, key_m, key_n, key_o, key_p, key_q, key_r, key_s, key_t, key_u, key_v, key_w, key_x, key_y, key_z;
    public Button key_0, key_1, key_2, key_3, key_4, key_5, key_6, key_7, key_8, key_9;
    public Button key_space, key_backspace, key_enter;

    public Transform targetTransform;


    void Start()
    {
        key_a.onClick.AddListener(() => { inputField.text += "a"; });
        key_b.onClick.AddListener(() => { inputField.text += "b"; });
        key_c.onClick.AddListener(() => { inputField.text += "c"; });
        key_d.onClick.AddListener(() => { inputField.text += "d"; });
        key_e.onClick.AddListener(() => { inputField.text += "e"; });
        key_f.onClick.AddListener(() => { inputField.text += "f"; });
        key_g.onClick.AddListener(() => { inputField.text += "g"; });
        key_h.onClick.AddListener(() => { inputField.text += "h"; });
        key_i.onClick.AddListener(() => { inputField.text += "i"; });
        key_j.onClick.AddListener(() => { inputField.text += "j"; });
        key_k.onClick.AddListener(() => { inputField.text += "k"; });
        key_l.onClick.AddListener(() => { inputField.text += "l"; });
        key_m.onClick.AddListener(() => { inputField.text += "m"; });
        key_n.onClick.AddListener(() => { inputField.text += "n"; });
        key_o.onClick.AddListener(() => { inputField.text += "o"; });
        key_p.onClick.AddListener(() => { inputField.text += "p"; });
        key_q.onClick.AddListener(() => { inputField.text += "q"; });
        key_r.onClick.AddListener(() => { inputField.text += "r"; });
        key_s.onClick.AddListener(() => { inputField.text += "s"; });
        key_t.onClick.AddListener(() => { inputField.text += "t"; });
        key_u.onClick.AddListener(() => { inputField.text += "u"; });
        key_v.onClick.AddListener(() => { inputField.text += "v"; });
        key_w.onClick.AddListener(() => { inputField.text += "w"; });
        key_x.onClick.AddListener(() => { inputField.text += "x"; });
        key_y.onClick.AddListener(() => { inputField.text += "y"; });
        key_z.onClick.AddListener(() => { inputField.text += "z"; });

        key_0.onClick.AddListener(() => { inputField.text += "0"; });
        key_1.onClick.AddListener(() => { inputField.text += "1"; });
        key_2.onClick.AddListener(() => { inputField.text += "2"; });
        key_3.onClick.AddListener(() => { inputField.text += "3"; });
        key_4.onClick.AddListener(() => { inputField.text += "4"; });
        key_5.onClick.AddListener(() => { inputField.text += "5"; });
        key_6.onClick.AddListener(() => { inputField.text += "6"; });
        key_7.onClick.AddListener(() => { inputField.text += "7"; });
        key_8.onClick.AddListener(() => { inputField.text += "8"; });
        key_9.onClick.AddListener(() => { inputField.text += "9"; });

        key_space.onClick.AddListener(() => { inputField.text += " "; });
        key_backspace.onClick.AddListener(() => { inputField.text = inputField.text.Substring(0, inputField.text.Length - 1); });   
        //key_enter.onClick.AddListener(() => { gameObject.SetActive(false); });        
        //ActivateKeyboard(inputField);

    }



    public void ActivateKeyboard(TMP_InputField _inputField)
    {
        inputField = _inputField;
        gameObject.SetActive(true);
        //inputField.Select(); inputField.ActivateInputField();
        //Orient the keyboard towards the target transform and position it in front of the camera
        transform.position = targetTransform.position + targetTransform.forward * 0.4f;
        transform.LookAt(targetTransform);

    }

    public void DeactivateKeyboard()
    {
        gameObject.SetActive(false);
    }

}