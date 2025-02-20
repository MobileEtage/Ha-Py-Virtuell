<?php

$from_mail = $_POST['email'];
$to_mail = "fedelheimer@die-etagen.de";
$body = $_POST['info'];
$imgName = $_POST['img'];
$imgData = $_FILES["imgData"]["tmp_name"];
//$uploadedData = $_FILES["uploadedData"]["tmp_name"];

// Multi attachment (need to verify)
//$dateien = array();
//$dateien[] = $uploadedData;
//$attachments = array();
// foreach($dateien AS $datei) { 
   // $name = basename($datei);
   // $size = filesize($datei);
   // $data = file_get_contents($datei);
   // $type = mime_content_type($datei);
   // $attachments[] = array("name"=>$name, "size"=>$size, "type"=>$type, "data"=>$data);
// }

// With attachment
$attachments = array();
$name = $imgName;
$size = filesize($imgData);
$data = file_get_contents($imgData);
$type = mime_content_type($imgData);
$attachments[] = array("name"=>$name, "size"=>$size, "type"=>$type, "data"=>$data);
//$body = utf8_decode($body);
mail_att($from_mail, $to_mail, "Treppen Intercon Angebot", $body, $attachments);

// Without attachment
//$headers = "From: ".$from_mail."\n";
//$headers.= "Content-Type: text/plain; charset=UTF-8";
//mail($target_mail, "Treppen Intercon Angebot", $body, $headers);

?>

<?php
function mail_att($from,$to,$subject,$message,$attachments)
{
	//$absender = "Mobile Etage";
	$absender_mail = $from;
	//$reply = "mobile@die-etagen.de";

	$mime_boundary = "-----=" . md5(uniqid(mt_rand(), 1));

	//$header  ="From:".$absender."<".$absender_mail.">\n";
	$header = "From: ".$from."\n";
	//$header .= "Reply-To: ".$reply."\n";

	$header.= "MIME-Version: 1.0\r\n";
	$header.= "Content-Type: multipart/mixed;\r\n";
	$header.= " boundary=\"".$mime_boundary."\"\r\n";

	$content = "This is a multi-part message in MIME format.\r\n\r\n";
	$content.= "--".$mime_boundary."\r\n";
	$content.= "Content-Type: text/plain charset=UTF-8\r\n";
	$content.= "Content-Transfer-Encoding: 8bit\r\n\r\n";
	$content.= $message."\r\n";
 
	//$anhang ist ein Mehrdimensionals Array
	//$anhang enthält mehrere Dateien
	if(is_array($attachments) AND is_array(current($attachments)))
	{
		foreach($attachments AS $dat)
		{
			$data = chunk_split(base64_encode($dat['data']));
			$content.= "--".$mime_boundary."\r\n";
			$content.= "Content-Disposition: attachment;\r\n";
			$content.= "\tfilename=\"".$dat['name']."\";\r\n";
			$content.= "Content-Length: .".$dat['size'].";\r\n";
			$content.= "Content-Type: ".$dat['type']."; name=\"".$dat['name']."\"\r\n";
			$content.= "Content-Transfer-Encoding: base64\r\n\r\n";
			$content.= $data."\r\n";
        }
		$content .= "--".$mime_boundary."--"; 
	}
	else //Nur 1 Datei als Anhang
    {
		$data = chunk_split(base64_encode($attachments['data']));

		$content.= "--".$mime_boundary."\r\n";
		$content.= "Content-Disposition: attachment;\r\n";
		$content.= "\tfilename=\"".$attachments['name']."\";\r\n";
		$content.= "Content-Length: .".$attachments['size'].";\r\n";
		$content.= "Content-Type: ".$attachments['type']."; name=\"".$attachments['name']."\"\r\n";
		$content.= "Content-Transfer-Encoding: base64\r\n\r\n";
		$content.= $data."\r\n";
    } 
	  
	//$charset = 'utf-8';
    //$content = htmlentities($content, ENT_COMPAT, $charset);
	  
    if(@mail($to, $subject, $content, $header)) return true;
    else return false;
   }
?>