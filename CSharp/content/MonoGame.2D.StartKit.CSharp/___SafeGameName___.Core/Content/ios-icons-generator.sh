#!/bin/zsh

# Declare a constant variables
readonly top_level_path="../../___SafeGameName___.iOS/AppIcon.xcassets"
readonly xcassets_path="$top_level_path/AppIcon.appiconset"

while true; do
  # Prompt for user confirmation
  echo -n "This script will delete your '$xcassets_path' directory and recreate all the assets again. Are you sure you wish to proceed? [(y)es/(n)o]: "
  read confirm

  # Check the user's response
  if [[ "$confirm" == "y" || "$confirm" == "yes" ]]; then
      # Check if the directory exists
      if [[ -d "$top_level_path" ]]; then
          # Top level directory exists, delete it
          rm -rf "$top_level_path"
          echo "'$top_level_path' directory deleted successfully."
      else
          echo "'$top_level_path' directory does not exist. Continuing."
      fi
      break
  elif [[ "$confirm" == "n" || "$confirm" == "no" ]]; then
      echo "Deletion canceled."
      exit 0
  else
      echo "Invalid input. Please enter 'y'/'yes' or 'n'/'no'."
  fi
done

echo "iOS Icon Generation Started!"

echo "Creating $xcassets_path directory"
mkdir -p "$xcassets_path"

# Generate the required icon sizes
echo "Generating iOS icons"
# Needed for Mac sips -Z 16 icon-1024.png -o "$xcassets_path/icon_16x16.png"
sips -Z 20 icon-1024.png -o "$xcassets_path/icon_20x20.png"
sips -Z 29 icon-1024.png -o "$xcassets_path/icon_29x29.png"
# Needed for Mac #sips -Z 32 icon-1024.png -o "$xcassets_path/icon_32x32.png"
sips -Z 40 icon-1024.png -o "$xcassets_path/icon_40x40.png"
sips -Z 58 icon-1024.png -o "$xcassets_path/icon_58x58.png"
sips -Z 60 icon-1024.png -o "$xcassets_path/icon_60x60.png"
# Needed for Mac #sips -Z 64 icon-1024.png -o "$xcassets_path/icon_64x64.png"
sips -Z 76 icon-1024.png -o "$xcassets_path/icon_76x76.png"
sips -Z 80 icon-1024.png -o "$xcassets_path/icon_80x80.png"
sips -Z 87 icon-1024.png -o "$xcassets_path/icon_87x87.png"
sips -Z 120 icon-1024.png -o "$xcassets_path/icon_120x120.png"
# Needed for Mac #sips -Z 128 icon-1024.png -o "$xcassets_path/icon_128x128.png"
sips -Z 152 icon-1024.png -o "$xcassets_path/icon_152x152.png"
sips -Z 167 icon-1024.png -o "$xcassets_path/icon_167x167.png"
sips -Z 180 icon-1024.png -o "$xcassets_path/icon_180x180.png"
# Needed for Mac #sips -Z 256 icon-1024.png -o "$xcassets_path/icon_256x256.png"
# Needed for Mac #sips -Z 512 icon-1024.png -o "$xcassets_path/icon_512x512.png"
# yes I know it's the same size
sips -Z 1024 icon-1024.png -o "$xcassets_path/icon_1024x1024.png"

# Create the Contents.json file
echo "Generating Contents.json file"
cat > "$xcassets_path/Contents.json" <<EOF
{
  "images" : [
    {
      "filename" : "icon_40x40.png",
      "idiom" : "iphone",
      "scale" : "2x",
      "size" : "20x20"
    },
    {
      "filename" : "icon_60x60.png",
      "idiom" : "iphone",
      "scale" : "3x",
      "size" : "20x20"
    },
    {
      "filename" : "icon_58x58.png",
      "idiom" : "iphone",
      "scale" : "2x",
      "size" : "29x29"
    },
    {
      "filename" : "icon_87x87.png",
      "idiom" : "iphone",
      "scale" : "3x",
      "size" : "29x29"
    },
    {
      "filename" : "icon_80x80.png",
      "idiom" : "iphone",
      "scale" : "2x",
      "size" : "40x40"
    },
    {
      "filename" : "icon_120x120.png",
      "idiom" : "iphone",
      "scale" : "3x",
      "size" : "40x40"
    },
    {
      "filename" : "icon_120x120.png",
      "idiom" : "iphone",
      "scale" : "2x",
      "size" : "60x60"
    },
    {
      "filename" : "icon_180x180.png",
      "idiom" : "iphone",
      "scale" : "3x",
      "size" : "60x60"
    },
    {
      "filename" : "icon_20x20.png",
      "idiom" : "ipad",
      "scale" : "1x",
      "size" : "20x20"
    },
    {
      "filename" : "icon_40x40.png",
      "idiom" : "ipad",
      "scale" : "2x",
      "size" : "20x20"
    },
    {
      "filename" : "icon_29x29.png",
      "idiom" : "ipad",
      "scale" : "1x",
      "size" : "29x29"
    },
    {
      "filename" : "icon_58x58.png",
      "idiom" : "ipad",
      "scale" : "2x",
      "size" : "29x29"
    },
    {
      "filename" : "icon_40x40.png",
      "idiom" : "ipad",
      "scale" : "1x",
      "size" : "40x40"
    },
    {
      "filename" : "icon_80x80.png",
      "idiom" : "ipad",
      "scale" : "2x",
      "size" : "40x40"
    },
    {
      "filename" : "icon_76x76.png",
      "idiom" : "ipad",
      "scale" : "1x",
      "size" : "76x76"
    },
    {
      "filename" : "icon_152x152.png",
      "idiom" : "ipad",
      "scale" : "2x",
      "size" : "76x76"
    },
    {
      "filename" : "icon_167x167.png",
      "idiom" : "ipad",
      "scale" : "2x",
      "size" : "83.5x83.5"
    },
    {
      "filename" : "icon_120x120.png",
      "idiom" : "car",
      "scale" : "2x",
      "size" : "60x60"
    },
    {
      "filename" : "icon_180x180.png",
      "idiom" : "car",
      "scale" : "3x",
      "size" : "60x60"
    },
    {
      "filename" : "icon_1024x1024.png",
      "idiom" : "ios-marketing",
      "scale" : "1x",
      "size" : "1024x1024"
    }
  ],
  "info" : {
    "author" : "xcode",
    "version" : 1
  }
}
EOF

echo "iOS Icon Generation Complete!"